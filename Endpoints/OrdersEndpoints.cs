using System.ComponentModel.DataAnnotations; // для ValidationResult, ValidationContext
using Microsoft.EntityFrameworkCore;        // для AnyAsync и других EF Core методов
using server.Models.Dtos;
using server.Data;
using server.Models;

namespace server.Endpoints
{
    public static class OrdersEndpoints
    {
        public static void MapOrdersEndpoints(this WebApplication app)
        {
            // POST /api/rides — создание новой поездки
            app.MapPost("/api/rides/create", async (RideCreateDto dto, AppDbContext db, ILogger<Program> logger) =>
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

                // Создаем объект поездки из DTO
                var ride = new Ride
                {
                    PassengerId = dto.PassengerId,
                    Passenger = passenger,
                    DriverId = dto.DriverId,  // Добавлено для полноты
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
                    return Results.Created($"/api/rides/create/{ride.Id}", new { ride });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating ride");
                    return Results.Problem("Internal server error");
                }
            });

            // GET /api/rides/passenger/{passengerId}/active — получить активную поездку пассажира
            app.MapGet("/api/rides/passenger/{passengerId:int}/active", async (int passengerId, AppDbContext db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Получен запрос активной поездки для пассажира {Id}", passengerId);
                try
                {
                    // Проверяем, существует ли пассажир (опционально, но полезно)
                    var passengerExists = await db.Passengers.AnyAsync(p => p.Id == passengerId);
                    if (!passengerExists)
                    {
                        logger.LogWarning("Passenger not found: {Id}", passengerId);
                        return Results.NotFound(new { Message = "Пассажир не найден" });
                    }

                    var ride = await db.Rides
                        .Where(r => r.PassengerId == passengerId)  // Исправлено на PassengerId
                        .FirstOrDefaultAsync();
                    return Results.Ok(ride);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching active ride for passenger {Id}", passengerId);
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });

            // PATCH /api/rides/{orderId}/price — увеличить цену заказа на указанную сумму
            app.MapPatch("/api/rides/{orderId:int}/price", async (int orderId, PriceUpdateRequest request, AppDbContext db, ILogger<Program> logger) =>
            {
                try
                {
                    var ride = await db.Rides.FindAsync(orderId);
                    if (ride == null)
                    {
                        logger.LogWarning("Попытка обновить цену несуществующего заказа с Id {OrderId}", orderId);
                        return Results.NotFound(new { Message = "Заказ не найден" });
                    }

                    if (request.Amount <= 0)
                    {
                        return Results.BadRequest(new { Message = "Сумма увеличения должна быть положительной" });
                    }

                    ride.Price += request.Amount;

                    await db.SaveChangesAsync();

                    logger.LogInformation("Цена заказа с Id {OrderId} увеличена на {Amount}. Новая цена: {NewPrice}", orderId, request.Amount, ride.Price);

                    return Results.Ok(new { Message = "Цена успешно обновлена", ride });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при обновлении цены заказа с Id {OrderId}", orderId);
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });

            // Новый эндпоинт для удаления заказа
            app.MapDelete("/api/rides/{orderId:int}", async (int orderId, AppDbContext db, ILogger<Program> logger) =>
            {
                try
                {
                    var order = await db.Rides.FindAsync(orderId);
                    if (order == null)
                    {
                        logger.LogWarning("Попытка удалить несуществующий заказ с Id {OrderId}", orderId);
                        return Results.NotFound(new { Message = "Заказ не найден" });
                    }

                    db.Rides.Remove(order);
                    await db.SaveChangesAsync();

                    logger.LogInformation("Заказ с Id {OrderId} успешно удалён", orderId);
                    return Results.Ok(new { Message = "Заказ успешно удалён" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при удалении заказа с Id {OrderId}", orderId);
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });



            // PUT /api/rides/{orderId}/accept — принять заказ водителем
            app.MapPut("/api/rides/{orderId:int}/accept", async (int orderId, AcceptOrderRequest request, AppDbContext db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Водитель {DriverId} пытается принять заказ {OrderId}", request.DriverId, orderId);

                try
                {
                    var ride = await db.Rides.FindAsync(orderId);
                    if (ride == null)
                    {
                        logger.LogWarning("Заказ с Id {OrderId} не найден", orderId);
                        return Results.NotFound(new { error = "Заказ не найден" });
                    }

                    // Проверяем, принял ли уже кто-то этот заказ
                    if (ride.DriverId != null)
                    {
                        logger.LogWarning("Заказ {OrderId} уже принят водителем {DriverIdExisting}", orderId, ride.DriverId);
                        return Results.BadRequest(new { error = "Заказ уже принят другим водителем" });
                    }

                    // Проверяем, что водитель существует (если есть таблица Drivers)
                    var driverExists = await db.Drivers.AnyAsync(d => d.Id == request.DriverId);
                    if (!driverExists)
                    {
                        logger.LogWarning("Водитель с Id {DriverId} не найден", request.DriverId);
                        return Results.BadRequest(new { error = "Водитель не найден" });
                    }

                    // Назначаем водителя и меняем статус
                    ride.DriverId = request.DriverId;
                    ride.Status = RideStatus.Ожидает; // или нужный статус принятия

                    await db.SaveChangesAsync();

                    logger.LogInformation("Заказ {OrderId} успешно принят водителем {DriverId}", orderId, request.DriverId);

                    return Results.Ok(new { msg = "Заказ принят" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при принятии заказа {OrderId} водителем {DriverId}", orderId, request.DriverId);
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });


            // GET /api/rides/driver/{driverId}/active — получить активную поездку водителя
            app.MapGet("/api/rides/driver/{driverId:int}/active", async (int driverId, AppDbContext db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Получен запрос активной поездки для водителя {Id}", driverId);
                try
                {
                    var driverExists = await db.Drivers.AnyAsync(d => d.Id == driverId);
                    if (!driverExists)
                    {
                        logger.LogWarning("Driver not found: {Id}", driverId);
                        return Results.NotFound(new { Message = "Водитель не найден" });
                    }

                    var ride = await db.Rides
                        .Where(r => r.DriverId == driverId)  // Исправлено на PassengerId
                        .FirstOrDefaultAsync();
                    return Results.Ok(ride);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при получении активной поездки для водителя {Id}", driverId);
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });

            // POST /api/orders/update-status — обновить статус заказа
            app.MapPut("/api/rides/{orderId:int}/status", async (int orderId, UpdateOrderStatusRequest request, AppDbContext db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Запрос на обновление статуса заказа {OrderId} на {NewStatus}", orderId, request.NewStatus);

                // Проверяем валидность статуса
                var validStatuses = new[] { RideStatus.Ожидает, RideStatus.Подъезжает, RideStatus.Впути };
                if (!validStatuses.Contains(request.NewStatus))
                {
                    logger.LogWarning("Недопустимый статус: {Status}", request.NewStatus);
                    return Results.BadRequest(new { error = "Недопустимый статус" });
                }

                try
                {
                    var ride = await db.Rides.FindAsync(orderId);
                    if (ride == null)
                    {
                        logger.LogWarning("Заказ с Id {OrderId} не найден", orderId);
                        return Results.NotFound(new { error = "Заказ не найден" });
                    }

                    // Обновляем статус
                    ride.Status = request.NewStatus;

                    await db.SaveChangesAsync();

                    logger.LogInformation("Статус заказа {OrderId} успешно обновлен на {Status}", orderId, request.NewStatus);
                    return Results.Ok(new { message = "Статус обновлен" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при обновлении статуса заказа {OrderId}", orderId);
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });

            app.MapGet("/api/rides/available", async (AppDbContext db, ILogger<Program> logger) =>
            {
                logger.LogInformation("Запрос списка доступных заказов (без назначенного водителя)");

                try
                {
                    var availableRides = await db.Rides
                        .Where(r => r.DriverId == null)  // Заказы без водителя
                        .ToListAsync();

                    return Results.Ok(availableRides);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при получении доступных заказов");
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });
        }
    }
}
