using server.Data;
using server.Models;
using server.Models.Dtos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;  // Add this line

namespace server.Services
{
    public class AdminnService : IAdminn
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public AdminnService(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            await Task.CompletedTask;

            // Получаем пароль из конфига (в открытом виде)
            var storedPassword = _configuration["Admin:Password"];
            if (string.IsNullOrEmpty(storedPassword))
            {
                throw new UnauthorizedAccessException("Пароль не настроен в конфигурации");
            }

            // Простое сравнение паролей
            if (request.Password != storedPassword)
            {
                throw new UnauthorizedAccessException("Неверный пароль");
            }

            // Если пароль верен — генерируем JWT-токен
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey не настроен"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, "Admin"),
                }),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "60")), // Используем ExpiryMinutes из конфига
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResponseDto { Token = tokenString };
        }
        // Новый метод: Получить водителей на проверке
        public async Task<List<DriverPendingDto>> GetPendingDriversAsync()
        {
            var baseUrl = "http://192.168.0.7:5251";  // Или используй IConfiguration для динамического URL
            return await _db.DriverDetails
                .Where(d => d.Status == DriverStatus.НаПроверке)
                .Select(d => new DriverPendingDto
                {
                    DriverId = d.DriverId,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    CarNumber = d.CarNumber,
                    DriverLicenseNumber = d.DriverLicenseNumber,
                    CarPhotoUrl = $"{baseUrl}{d.CarPhotoUrl}",
                    DriverLicensePhotoUrl = $"{baseUrl}{d.DriverLicensePhotoUrl}"
                })
                .ToListAsync();
        }

        public async Task<List<DriverActiveDto>> GetActiveDriversAsync()
        {
            var baseUrl = "http://192.168.0.7:5251";  // Или используй IConfiguration для динамического URL
            return await _db.DriverDetails
                .Where(d => d.Status == DriverStatus.Активен)
                .Select(d => new DriverActiveDto
                {
                    DriverId = d.DriverId,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    CarNumber = d.CarNumber,
                    DriverLicenseNumber = d.DriverLicenseNumber,
                    CarPhotoUrl = $"{baseUrl}{d.CarPhotoUrl}",
                    DriverLicensePhotoUrl = $"{baseUrl}{d.DriverLicensePhotoUrl}",
                    Status = d.Status.ToString()  // Добавляем статус как строку
                })
                .ToListAsync();
        }

        // Новый метод: Обновить статус
        public async Task UpdateDriverStatusAsync(int driverId, DriverStatus newStatus)
        {
            var driver = await _db.DriverDetails.FindAsync(driverId);
            if (driver == null)
                throw new KeyNotFoundException("Водитель не найден");

            driver.Status = newStatus;
            await _db.SaveChangesAsync();
        }

        public async Task<PricingSettings> GetPricingSettingsAsync()
        {
            // Предполагается, что запись с Id=1 всегда существует
            var pricing = await _db.PricingSettings.FindAsync(1);
            if (pricing == null)
            {
                // Если нет, возвращаем дефолтные значения
                return new PricingSettings();
            }
            return pricing;
        }
        public async Task UpdatePricingSettingsAsync(UpdatePricingSettingsDto dto)
        {
            // Найти запись с Id=1 (или создать, если не существует)
            var existing = await _db.PricingSettings.FindAsync(1);
            if (existing == null)
            {
                // Если записи нет, создаём новую
                var newPricing = new PricingSettings
                {
                    Id = 1,
                    BasePrice = dto.BasePrice,
                    PricePerKm = dto.PricePerKm,
                    NightMultiplier = dto.NightMultiplier,
                    BadWeatherMultiplier = dto.BadWeatherMultiplier
                };
                _db.PricingSettings.Add(newPricing);
            }
            else
            {
                // Обновляем существующую
                existing.BasePrice = dto.BasePrice;
                existing.PricePerKm = dto.PricePerKm;
                existing.NightMultiplier = dto.NightMultiplier;
                existing.BadWeatherMultiplier = dto.BadWeatherMultiplier;
            }
            await _db.SaveChangesAsync();
        }

    }
}
