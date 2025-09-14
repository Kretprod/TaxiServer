using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using server.Data;
using server.Hubs;
using server.Models;
using server.Models.Dtos;
using System.ComponentModel.DataAnnotations;

namespace server.Services
{
    public class RideService : IRideService
    {
        private readonly AppDbContext _db; // Контекст базы данных
        private readonly ILogger<RideService> _logger; // Логгер для логирования событий и ошибок
        private readonly IHubContext<OrdersHub> _ordersHub; // SignalR хаб для уведомлений клиентов

        // Конструктор с внедрением зависимостей
        public RideService(AppDbContext db, ILogger<RideService> logger, IHubContext<OrdersHub> ordersHub)
        {
            _db = db;
            _logger = logger;
            _ordersHub = ordersHub;
        }

        // Создаёт новую поездку на основе DTO с валидацией входных данных
        public async Task<(bool Success, IEnumerable<string>? Errors, Ride? Ride)> CreateRideAsync(RideCreateDto dto)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(dto);

            // Валидация DTO по DataAnnotations
            if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage ?? "Неизвестная ошибка");
                return (false, errors, null);
            }

            // Проверка существования пассажира
            var passenger = await _db.Passengers.FindAsync(dto.PassengerId);
            if (passenger == null)
            {
                return (false, new[] { "Пассажир не найден" }, null);
            }

            // Создаём объект поездки с данными из DTO
            var ride = new Ride
            {
                PassengerId = dto.PassengerId,
                Passenger = passenger,
                DriverId = dto.DriverId,
                PickupLocation = dto.PickupLocation,
                PickupLatitude = dto.PickupLatitude,
                PickupLongitude = dto.PickupLongitude,
                DropoffLocation = dto.DropoffLocation,
                DropoffLatitude = dto.DropoffLatitude,
                DropoffLongitude = dto.DropoffLongitude,
                Price = dto.Price,
                Distance = dto.Distance,
                PaymentMethod = Enum.Parse<PaymentMethod>(dto.PaymentMethod, ignoreCase: true),
                Status = RideStatus.Ищет,
            };

            try
            {
                _db.Rides.Add(ride);
                await _db.SaveChangesAsync();

                return (true, null, ride);
            }
            catch (Exception)
            {
                return (false, new[] { "Внутренняя ошибка сервера" }, null);
            }
        }


        /// Получает активную поездку для пассажира 
        public async Task<Ride?> GetActiveRideForPassengerAsync(int passengerId)
        {
            return await _db.Rides
                        .Where(r => r.PassengerId == passengerId)
                        .FirstOrDefaultAsync();
        }

        // Получает активную поездку для водителя 
        public async Task<Ride?> GetActiveRideForDriverAsync(int driverId)
        {
            return await _db.Rides
                        .Where(r => r.DriverId == driverId)
                        .FirstOrDefaultAsync();
        }

        /// Обновляет цену поездки
        public async Task<(bool Success, string? ErrorMessage, Ride? UpdatedRide)> UpdateRidePriceAsync(int orderId, decimal amount)
        {
            var ride = await _db.Rides.FindAsync(orderId);
            if (ride == null)
            {
                return (false, "Заказ не найден", null);
            }

            ride.Price += amount;

            try
            {
                await _db.SaveChangesAsync();

                return (true, null, ride);
            }
            catch (Exception)
            {
                return (false, "Внутренняя ошибка сервера", null);
            }
        }

        /// Удаляет поездку
        public async Task<(bool Success, string? ErrorMessage)> DeleteRideAsync(int orderId)
        {
            var ride = await _db.Rides.FindAsync(orderId);
            if (ride == null)
                return (false, "Заказ не найден");

            _db.Rides.Remove(ride);

            try
            {
                await _db.SaveChangesAsync();

                // Оповещение всех клиентов об удалении поездки
                await _ordersHub.Clients.Group($"order_{orderId}").SendAsync("OrderStatusChanged", orderId);

                return (true, null);
            }
            catch (Exception)
            {
                return (false, "Внутренняя ошибка сервера");
            }
        }

        // Получает историю поездок пользователя по роли (водитель или пассажир)
        public async Task<(bool Success, IEnumerable<RideHistory>? History, string? ErrorMessage)> GetRideHistoryAsync(int userId, string role)
        {
            try
            {
                IQueryable<RideHistory> query = _db.RideHistories;


                if (role.ToLower() == "driver")
                {
                    query = query.Where(r => r.DriverId == userId);
                }
                else if (role.ToLower() == "passenger")
                {
                    query = query.Where(r => r.PassengerId == userId);
                }
                else
                {
                    return (false, null, "Параметр role должен быть 'driver' или 'passenger'");
                }

                var rideHistory = await query.OrderByDescending(r => r.CompletedAt).ToListAsync();
                return (true, rideHistory, null);
            }
            catch (Exception)
            {
                return (false, null, "Внутренняя ошибка сервера");
            }
        }

        // Водитель принимает заказ, если он ещё не принят
        public async Task<(bool Success, string? ErrorMessage)> AcceptOrderAsync(int orderId, int driverId)
        {
            try
            {
                var ride = await _db.Rides.FindAsync(orderId);
                if (ride == null)
                {
                    return (false, "Заказ не найден");
                }

                // Проверяем, принял ли уже кто-то этот заказ
                if (ride.DriverId != null)
                {
                    return (false, "Заказ уже принят другим водителем");
                }

                // Проверяем, что водитель существует 
                var driverExists = await _db.Drivers.AnyAsync(d => d.Id == driverId);
                if (!driverExists)
                {
                    return (false, "Водитель не найден");
                }

                // Назначаем водителя и меняем статус
                ride.DriverId = driverId;
                ride.Status = RideStatus.Ожидает; // или нужный статус принятия

                await _db.SaveChangesAsync();

                // Отправляем уведомление всем подписанным клиентам
                await _ordersHub.Clients.Group($"order_{orderId}").SendAsync("OrderStatusChanged", orderId, ride.Status.ToString());

                return (true, null);
            }
            catch (Exception)
            {
                return (false, "Внутренняя ошибка сервера");
            }
        }

        /// Обновляет статус заказа
        public async Task<(bool Success, string? ErrorMessage)> UpdateOrderStatusAsync(int orderId, RideStatus newStatus)
        {
            // Проверяем валидность статуса
            var validStatuses = new[] { RideStatus.Ожидает, RideStatus.Подъезжает, RideStatus.Впути };
            if (!validStatuses.Contains(newStatus))
            {
                return (false, "Недопустимый статус");
            }

            try
            {
                var ride = await _db.Rides.FindAsync(orderId);
                if (ride == null)
                {
                    return (false, "Заказ не найден");
                }

                // Обновляем статус
                ride.Status = newStatus;

                await _db.SaveChangesAsync();

                // Отправляем уведомление всем подписанным клиентам
                await _ordersHub.Clients.Group($"order_{orderId}").SendAsync("OrderStatusChanged", orderId, newStatus.ToString());

                return (true, null);
            }
            catch (Exception)
            {
                return (false, "Внутренняя ошибка сервера");
            }
        }

        //Завершает заказ
        public async Task<(bool Success, string? ErrorMessage)> CompleteOrderAsync(int orderId)
        {
            try
            {
                var ride = await _db.Rides.FindAsync(orderId);
                if (ride == null)
                {
                    return (false, "Заказ не найден");
                }

                var rideHistory = new RideHistory
                {
                    PassengerId = ride.PassengerId,
                    DriverId = ride.DriverId,
                    PickupLocation = ride.PickupLocation,
                    PickupLatitude = ride.PickupLatitude,
                    PickupLongitude = ride.PickupLongitude,
                    DropoffLocation = ride.DropoffLocation,
                    DropoffLatitude = ride.DropoffLatitude,
                    DropoffLongitude = ride.DropoffLongitude,
                    Price = ride.Price,
                    Distance = ride.Distance,
                    PaymentMethod = ride.PaymentMethod,
                    CompletedAt = DateTime.UtcNow
                };

                _db.RideHistories.Add(rideHistory);
                _db.Rides.Remove(ride);
                await _db.SaveChangesAsync();

                // Отправляем уведомление всем подписанным клиентам
                await _ordersHub.Clients.Group($"order_{orderId}").SendAsync("OrderStatusChanged", orderId);

                return (true, null);
            }
            catch (Exception)
            {
                return (false, "Внутренняя ошибка сервера");
            }
        }

        // Получает список доступных поездок (без водителя и со статусом "Ищет")
        public async Task<IEnumerable<Ride>> GetAvailableRidesAsync()
        {
            return await _db.Rides
                        .Where(r => r.DriverId == null)  // Заказы без водителя
                        .ToListAsync();
        }
    }
}
