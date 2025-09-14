using server.Models.Dtos;
using server.Models;

namespace server.Services
{
    // Интерфейс для сервиса работы с поездками
    public interface IRideService
    {
        // Создание новой поездки с возвратом результата и созданной сущности
        Task<(bool Success, IEnumerable<string>? Errors, Ride? Ride)> CreateRideAsync(RideCreateDto dto);

        // Получение активной поездки для пассажира
        Task<Ride?> GetActiveRideForPassengerAsync(int passengerId);

        // Получение активной поездки для водителя
        Task<Ride?> GetActiveRideForDriverAsync(int driverId);

        // Обновление цены поездки
        Task<(bool Success, string? ErrorMessage, Ride? UpdatedRide)> UpdateRidePriceAsync(int orderId, decimal amount);

        // Удаление поездки
        Task<(bool Success, string? ErrorMessage)> DeleteRideAsync(int orderId);

        // Получение истории поездок пользователя по роли
        Task<(bool Success, IEnumerable<RideHistory>? History, string? ErrorMessage)> GetRideHistoryAsync(int userId, string role);

        // Принятие заказа водителем
        Task<(bool Success, string? ErrorMessage)> AcceptOrderAsync(int orderId, int driverId);

        // Обновление статуса заказа
        Task<(bool Success, string? ErrorMessage)> UpdateOrderStatusAsync(int orderId, RideStatus newStatus);

        // Завершение заказа с переносом в историю
        Task<(bool Success, string? ErrorMessage)> CompleteOrderAsync(int orderId);

        // Получение списка доступных поездок (без водителя)
        Task<IEnumerable<Ride>> GetAvailableRidesAsync();
    }
}
