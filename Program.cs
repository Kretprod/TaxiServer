using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; 
using System.ComponentModel.DataAnnotations;
using server.Data;
using server.Models;
using server.Services;
using server.Models.Dtos;
using server.Hubs; 

var builder = WebApplication.CreateBuilder(args);

// Регистрируем контекст базы данных с PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрируем SignalR для real-time коммуникаций
builder.Services.AddSignalR();

// Регистрируем сервис для отправки email (EmailService)
builder.Services.AddScoped<EmailService>();

// Настройка CORS — разрешаем любые заголовки, методы и источники (для разработки)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

// Добавляем логирование в консоль
builder.Logging.AddConsole();

var app = builder.Build();

app.UseCors(); // Включаем CORS

// --- Минимальные API ---

// POST /api/rides — создание новой поездки
app.MapPost("/api/rides", async (RideCreateDto dto, AppDbContext db, ILogger<Program> logger) =>
{
    // Валидация входящего DTO (данных запроса)
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        logger.LogWarning("Validation failed for RideCreateDto: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
        return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
    }

    // Проверяем, существует ли пассажир с указанным Id
    var passenger = await db.Passengers.FindAsync(dto.PassengerId);
    if (passenger == null)
    {
        return Results.BadRequest(new { Message = "Passenger not found" });
    }

    // Проверяем, есть ли у пассажира уже активная поездка (Ищет или Ожидает)
    var rideExists = await db.Rides.AnyAsync(r => r.PassengerId == dto.PassengerId && (r.Status == RideStatus.Ищет || r.Status == RideStatus.Ожидает));
    if (rideExists)
    {
        return Results.BadRequest(new { Message = "У пассажира уже есть активная поездка" });
    }

    // Создаем объект поездки из DTO
    var ride = new Ride
    {
        PassengerId = dto.PassengerId,
        Passenger = passenger, 
        PickupLocation = dto.PickupLocation,
        PickupLatitude = dto.PickupLatitude,
        PickupLongitude = dto.PickupLongitude,
        DropoffLocation = dto.DropoffLocation,
        DropoffLatitude = dto.DropoffLatitude,
        DropoffLongitude = dto.DropoffLongitude,
        Price = dto.Price,
        Distance = dto.Distance,
        PaymentMethod = Enum.Parse<PaymentMethod>(dto.PaymentMethod), // Преобразуем строку в enum
        Status = RideStatus.Ищет // Начальный статус поездки
    };

    try
    {
        // Добавляем и сохраняем поездку в базе
        db.Rides.Add(ride);
        await db.SaveChangesAsync();
        logger.LogInformation("Ride created with ID: {Id}", ride.Id);
        return Results.Created($"/api/rides/{ride.Id}", ride);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating ride");
        return Results.Problem("Internal server error");
    }
});

// GET /api/rides/passenger/{passengerId} — получить поездку пассажира
app.MapGet("/api/rides/passenger/{passengerId:int}", async (int passengerId, AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        // Получаем первую поездку пассажира, включая данные водителя
        var ride = await db.Rides
            .Where(r => r.PassengerId == passengerId)
            .Include(r => r.Driver)
            .FirstOrDefaultAsync();

        if (ride == null)
        {
            return Results.NotFound(new { Message = "Поездка для данного пассажира не найдена" });
        }

        return Results.Ok(ride);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching ride for passenger {Id}", passengerId);
        return Results.Problem("Internal server error");
    }
});

// POST /api/rides/accept — принятие заказа водителем
app.MapPost("/api/rides/accept", async (AcceptRideRequest request, AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        // Найти заказ по rideId
        var ride = await db.Rides.FindAsync(request.RideId);
        if (ride == null)
            return Results.NotFound(new { Message = "Заказ не найден" });

        // Проверить, что водитель ещё не назначен
        if (ride.DriverId != null)
            return Results.BadRequest(new { Message = "Заказ уже принят другим водителем" });

        // Назначить водителя и обновить статус
        ride.DriverId = request.DriverId;
        ride.Status = RideStatus.Ожидает;

        await db.SaveChangesAsync();

        return Results.Ok(new { Message = "Заказ принят", RideId = ride.Id, DriverId = ride.DriverId, Status = ride.Status });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error accepting ride {RideId} by driver {DriverId}", request.RideId, request.DriverId);
        return Results.Problem("Internal server error");
    }
});

// GET /api/rides/driver/{driverId} — получить поездку водителя
app.MapGet("/api/rides/driver/{driverId:int}", async (int driverId, AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        // Получаем первую поездку водителя, включая данные пассажира
        var ride = await db.Rides
            .Where(r => r.DriverId == driverId)
            .Include(r => r.Passenger)
            .FirstOrDefaultAsync();

        if (ride == null)
        {
            return Results.NotFound(new { Message = "Поездка для данного водителя не найдена" });
        }

        return Results.Ok(ride);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching ride for driver {Id}", driverId);
        return Results.Problem("Internal server error");
    }
});

// POST /api/auth/send-code — отправить код подтверждения на email
app.MapPost("/api/auth/send-code", async (SendCodeRequest request, AppDbContext db, EmailService emailService, ILogger<Program> logger) =>
{
    // Валидация запроса
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        logger.LogWarning("Validation failed for SendCodeRequest: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
        return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
    }

    // Проверка роли (дополнительная валидация)
    if (request.Role != "passenger" && request.Role != "driver")
    {
        return Results.BadRequest(new { Message = "Invalid role" });
    }

    // Ограничение: не более 10 кодов в час на один email
    var oneHourAgo = DateTime.UtcNow.AddHours(-1);
    var recentCodes = await db.VerificationCodes
        .Where(v => v.Email == request.Email && v.CreatedAt > oneHourAgo)
        .ToListAsync();
    if (recentCodes.Count >= 10)
    {
        logger.LogWarning("Too many requests for {Email}", request.Email);
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    // Генерируем 6-значный код
    var code = new Random().Next(100000, 999999).ToString();

    var verificationCode = new VerificationCode
    {
        Email = request.Email,
        Code = code,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(10) // Код действителен 10 минут
    };

    try
    {
        // Удаляем старые неиспользованные коды для этого email
        var oldCodes = await db.VerificationCodes
            .Where(v => v.Email == request.Email && !v.IsUsed)
            .ToListAsync();
        db.VerificationCodes.RemoveRange(oldCodes);

        // Добавляем новый код в базу
        db.VerificationCodes.Add(verificationCode);
        await db.SaveChangesAsync();

        // Отправляем email с кодом
        await emailService.SendVerificationCodeAsync(request.Email, code);
        logger.LogInformation("Verification code sent to {Email}", request.Email);
        return Results.Ok(new { Message = "Code sent successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error sending code to {Email}", request.Email);
        return Results.Problem("Failed to send code");
    }
});

// POST /api/auth/confirm — подтвердить регистрацию по коду
app.MapPost("/api/auth/confirm", async (ConfirmRegistrationRequest request, AppDbContext db, ILogger<Program> logger) =>
{
    // Валидация входных данных
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        logger.LogWarning("Validation failed for ConfirmRegistrationRequest: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
        return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
    }

    // Проверка роли
    if (request.Role != "passenger" && request.Role != "driver")
    {
        return Results.BadRequest(new { Message = "Invalid role" });
    }

    // Ищем код в базе, который совпадает с email и кодом, и не использован
    var verificationCode = await db.VerificationCodes
        .FirstOrDefaultAsync(v => v.Email == request.Email && v.Code == request.Code && !v.IsUsed);

    if (verificationCode == null)
    {
        logger.LogWarning("Invalid or expired code for {Email}", request.Email);
        return Results.BadRequest(new { Message = "Invalid or expired code" });
    }

    // Проверяем срок действия кода
    if (DateTime.UtcNow > verificationCode.ExpiresAt)
    {
        logger.LogWarning("Code expired for {Email}", request.Email);
        return Results.BadRequest(new { Message = "Code expired" });
    }

    // Помечаем код как использованный
    verificationCode.IsUsed = true;
    await db.SaveChangesAsync();

    // Проверяем, есть ли уже пользователь с таким email (пассажир или водитель)
    var existingPassenger = await db.Passengers.FirstOrDefaultAsync(p => p.Email == request.Email);
    var existingDriver = await db.Drivers.FirstOrDefaultAsync(d => d.Email == request.Email);
    if (existingPassenger != null || existingDriver != null)
    {
        logger.LogInformation("User already exists: {Email}", request.Email);
        return Results.Ok(new { Message = "Already registered" });
    }

    try
    {
        if (request.Role == "passenger")
        {
            // Создаем нового пассажира
            var passenger = new Passenger
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone
            };
            db.Passengers.Add(passenger);
            await db.SaveChangesAsync();
            logger.LogInformation("Passenger registered: {Email}", request.Email);
            return Results.Ok(new { Message = "Registration confirmed", UserId = passenger.Id, Role = "passenger" });
        }
        else if (request.Role == "driver")
        {
            // Создаем нового водителя
            var driver = new Driver
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone
            };
            db.Drivers.Add(driver);
            await db.SaveChangesAsync();
            logger.LogInformation("Driver registered: {Email}", request.Email);
            return Results.Ok(new { Message = "Registration confirmed", UserId = driver.Id, Role = "driver" });
        }
        else
        {
            return Results.BadRequest(new { Message = "Invalid role" });
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error registering user {Email}", request.Email);
        return Results.Problem("Internal server error");
    }
});

// POST /api/orders/update-status — обновить статус заказа
app.MapPost("/api/orders/update-status", async (UpdateOrderStatusRequest request, AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        // Найти заказ
        var order = await db.Rides.FindAsync(request.OrderId);
        if (order == null)
            return Results.NotFound(new { Message = "Заказа нет" });

        // Валидация статуса
        if (!Enum.IsDefined(typeof(RideStatus), request.NewStatus))
        {
            return Results.BadRequest(new { Message = "Недопустимое значение статуса" });
        }

        // Обновить статус
        order.Status = request.NewStatus;

        // Сохранить изменения
        await db.SaveChangesAsync();
        logger.LogInformation("Order {OrderId} status updated to {Status}", request.OrderId, request.NewStatus);
        return Results.Ok(new { Message = "Статус обновлён" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating status for order {OrderId}", request.OrderId);
        return Results.Problem("Internal server error");
    }
});

// Подключаем SignalR хаб для real-time обновлений заказов
app.MapHub<OrdersHub>("/ordersHub");

app.Run();
