using Microsoft.AspNetCore.SignalR;
using server.Models.Dtos;
using server.Services;
using server.Models;

namespace server.Endpoints
{
    public static class OrdersEndpoints
    {
        // Метод расширения для регистрации всех маршрутов, связанных с заказами (поездками)
        public static void MapOrdersEndpoints(this WebApplication app)
        {
            // Создание новой поездки
            app.MapPost("/api/rides/create", async (RideCreateDto dto, IRideService rideService) =>
            {
                // Вызываем сервис для создания поездки
                var (success, errors, ride) = await rideService.CreateRideAsync(dto);
                if (!success)
                    // Возвращаем ошибку 400 с деталями ошибок, если что-то не так
                    return Results.BadRequest(new { Errors = errors });
                // Возвращаем 201 Created с данными созданной поездки
                return Results.Created($"/api/rides/create/{ride!.Id}", new { ride });
            });

            // Получение активной поездки для пассажира
            app.MapGet("/api/rides/passenger/{passengerId:int}/active", async (int passengerId, IRideService rideService) =>
            {
                var ride = await rideService.GetActiveRideForPassengerAsync(passengerId);
                return Results.Ok(ride);
            });

            // Обновление цены поездки
            app.MapPatch("/api/rides/{orderId:int}/price", async (int orderId, PriceUpdateRequest request, IRideService rideService) =>
            {
                var (success, error, updatedRide) = await rideService.UpdateRidePriceAsync(orderId, request.Amount);
                if (!success)
                    return Results.BadRequest(new { Message = error });
                return Results.Ok(new { ride = updatedRide });
            });

            // Удаление поездки
            app.MapDelete("/api/rides/{orderId:int}", async (int orderId, IRideService rideService) =>
            {
                var (success, error) = await rideService.DeleteRideAsync(orderId);
                if (!success)
                    return Results.NotFound(new { Message = error });
                return Results.Ok(new { Message = "Заказ успешно удалён" });
            });

            // Получение истории поездок пользователя (водителя или пассажира)
            app.MapGet("/api/rides/history/{userId:int}", async (int userId, HttpRequest request, IRideService rideService) =>
            {
                var role = request.Query["role"].ToString();

                if (string.IsNullOrWhiteSpace(role))
                {
                    return Results.BadRequest("Параметр 'role' обязателен и не может быть пустым.");
                }
                try
                {
                    var (success, history, error) = await rideService.GetRideHistoryAsync(userId, role);
                    return Results.Ok(history);
                }
                catch (ArgumentException ex)
                {
                    // Если роль указана неверно — возвращаем 400 с сообщением
                    return Results.BadRequest(ex.Message);
                }
            });

            // Принятие заказа водителем
            app.MapPut("/api/rides/{orderId:int}/accept", async (int orderId, AcceptOrderRequest request, IRideService rideService) =>
            {
                var (success, error) = await rideService.AcceptOrderAsync(orderId, request.DriverId);
                if (!success)
                    return Results.BadRequest(new { error });
                return Results.Ok(new { msg = "Заказ принят" });
            });

            // Получение активной поездки для водителя
            app.MapGet("/api/rides/driver/{driverId:int}/active", async (int driverId, IRideService rideService) =>
            {
                var ride = await rideService.GetActiveRideForDriverAsync(driverId);
                if (ride == null)
                    return Results.NotFound(new { Message = "Водитель не найден или нет активной поездки" });
                return Results.Ok(ride);
            });

            // Обновление статуса заказа (например, "Ожидает", "Подъезжает" и т.д.)
            app.MapPut("/api/rides/{orderId:int}/status", async (int orderId, UpdateOrderStatusRequest request, IRideService rideService) =>
            {
                var (success, error) = await rideService.UpdateOrderStatusAsync(orderId, request.NewStatus);
                if (!success)
                    return Results.BadRequest(new { error });
                return Results.Ok(new { message = "Статус обновлен" });
            });

            // Завершение заказа (перемещение в историю)
            app.MapPost("/api/rides/{orderId:int}/complete", async (int orderId, IRideService rideService) =>
            {
                var (success, error) = await rideService.CompleteOrderAsync(orderId);
                if (!success)
                    return Results.NotFound(new { Message = error });
                return Results.Ok(new { Message = "Заказ успешно завершён" });
            });

            // Получение списка доступных поездок (без назначенного водителя)
            app.MapGet("/api/rides/available", async (IRideService rideService) =>
            {
                var rides = await rideService.GetAvailableRidesAsync();
                return Results.Ok(rides);
            });
        }
    }
}
