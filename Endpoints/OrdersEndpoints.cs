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
                    return Results.Created($"/api/rides/create/{ride.Id}", new { ride = ride });
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

            // POST /api/rides/accept — принятие заказа водителем
            app.MapPost("/api/rides/accept", async (AcceptRideRequest request, AppDbContext db, ILogger<Program> logger) =>
            {
                try
                {
                    // Найти заказ по RideId
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
                        .Where(r => r.DriverId == driverId)  // Исправлено на DriverId
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
        }
    }
}
