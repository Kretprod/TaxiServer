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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();

builder.Services.AddScoped<EmailService>();

// CORS 
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});

builder.Logging.AddConsole();

var app = builder.Build();

app.UseCors();

// Минимальные API

// POST /api/rides — Создать поездку (использует RideCreateDto)
app.MapPost("/api/rides", async (RideCreateDto dto, AppDbContext db, ILogger<Program> logger) =>
{
    // Валидация DTO
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(dto);
    if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
    {
        logger.LogWarning("Validation failed for RideCreateDto: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
        return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
    }
    var passenger = await db.Passengers.FindAsync(dto.PassengerId);
    if (passenger == null)
    {
        return Results.BadRequest(new { Message = "Passenger not found" });
    }
    // Маппинг DTO в модель Ride
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
        PaymentMethod = Enum.Parse<PaymentMethod>(dto.PaymentMethod), 
        Status = RideStatus.Ищет 
    };

    try
    {
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

// GET /api/rides/{passengerId} — Получить поездки пассажира
app.MapGet("/api/rides/{passengerId:int}", async (int passengerId, AppDbContext db, ILogger<Program> logger) =>
{
    try
    {
        var rides = await db.Rides
            .Where(r => r.PassengerId == passengerId)
            .Include(r => r.Driver)
            .ToListAsync();
        return Results.Ok(rides);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching rides for passenger {Id}", passengerId);
        return Results.Problem("Internal server error");
    }
});

// POST /api/auth/send-code — Отправить код подтверждения (использует SendCodeRequest)
app.MapPost("/api/auth/send-code", async (SendCodeRequest request, AppDbContext db, EmailService emailService, ILogger<Program> logger) =>
{
    // Валидация
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        logger.LogWarning("Validation failed for SendCodeRequest: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
        return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
    }

    // Проверка лимита отправок (не более 10 в час)
    var oneHourAgo = DateTime.UtcNow.AddHours(-1);
    var recentCodes = await db.VerificationCodes
        .Where(v => v.Email == request.Email && v.CreatedAt > oneHourAgo)
        .ToListAsync();
    if (recentCodes.Count >= 10)
    {
        logger.LogWarning("Too many requests for {Email}", request.Email);
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    // Генерация кода
    var code = new Random().Next(100000, 999999).ToString();
    var verificationCode = new VerificationCode
    {
        Email = request.Email,
        Code = code,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(10) // Действителен 10 минут
    };

    try
    {
        // Удаляем старые неиспользованные коды для этого email
        var oldCodes = await db.VerificationCodes
            .Where(v => v.Email == request.Email && !v.IsUsed)
            .ToListAsync();
        db.VerificationCodes.RemoveRange(oldCodes);

        // Сохраняем новый код
        db.VerificationCodes.Add(verificationCode);
        await db.SaveChangesAsync();

        // Отправляем email
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

// POST /api/auth/confirm — Подтвердить регистрацию (использует ConfirmRegistrationRequest)
app.MapPost("/api/auth/confirm", async (ConfirmRegistrationRequest request, AppDbContext db, ILogger<Program> logger) =>
{
    // Валидация
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        logger.LogWarning("Validation failed for ConfirmRegistrationRequest: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
        return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
    }

    // Ищем код в БД
    var verificationCode = await db.VerificationCodes
        .FirstOrDefaultAsync(v => v.Email == request.Email && v.Code == request.Code && !v.IsUsed);

    if (verificationCode == null)
    {
        logger.LogWarning("Invalid or expired code for {Email}", request.Email);
        return Results.BadRequest(new { Message = "Invalid or expired code" });
    }

    // Проверяем время жизни
    if (DateTime.UtcNow > verificationCode.ExpiresAt)
    {
        logger.LogWarning("Code expired for {Email}", request.Email);
        return Results.BadRequest(new { Message = "Code expired" });
    }

    // Помечаем код как использованный
    verificationCode.IsUsed = true;
    await db.SaveChangesAsync();

    // Создаём пассажира (или возвращаем существующего)
    var existingPassenger = await db.Passengers.FirstOrDefaultAsync(p => p.Email == request.Email);
    if (existingPassenger != null)
    {
        logger.LogInformation("Passenger already exists: {Email}", request.Email);
        return Results.Ok(new { Message = "Already registered", PassengerId = existingPassenger.Id });
    }

    var passenger = new Passenger
    {
        Name = request.Name,
        Email = request.Email,
        Phone = request.Phone
    };

    try
    {
        db.Passengers.Add(passenger);
        await db.SaveChangesAsync();
        logger.LogInformation("Passenger registered: {Email}", request.Email);
        return Results.Ok(new { Message = "Registration confirmed", PassengerId = passenger.Id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error registering passenger {Email}", request.Email);
        return Results.Problem("Internal server error");
    }
});

// SignalR хаб
app.MapHub<OrdersHub>("/ordersHub");

app.Run();
