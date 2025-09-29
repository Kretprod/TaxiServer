using System.ComponentModel.DataAnnotations;

namespace server.Models.Dtos
{
    public class LoginRequestDto
    {
        [Required]  // Добавил валидацию — пароль обязателен
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }

    // Новый DTO для ответа списка водителей
    public class DriverPendingDto
    {
        public int DriverId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CarNumber { get; set; } = string.Empty;
        public string DriverLicenseNumber { get; set; } = string.Empty;
        public string CarPhotoUrl { get; set; } = string.Empty;
        public string DriverLicensePhotoUrl { get; set; } = string.Empty;
    }

    // Новый DTO для обновления статуса
    public class StatusUpdateDto
    {
        [Required]
        public DriverStatus Status { get; set; }  // Активен или Неактивен
    }
    public class DriverActiveDto
    {
        public int DriverId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CarNumber { get; set; } = string.Empty;
        public string DriverLicenseNumber { get; set; } = string.Empty;
        public string CarPhotoUrl { get; set; } = string.Empty;
        public string DriverLicensePhotoUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;  // Добавлено для отображения статуса
    }
    public class UpdatePricingSettingsDto
    {
        public decimal BasePrice { get; set; }  // Базовая цена
        public decimal PricePerKm { get; set; }  // Цена за км
        public decimal NightMultiplier { get; set; }  // Ночной множитель
        public decimal BadWeatherMultiplier { get; set; }  // Множитель за плохую погоду
    }
}
