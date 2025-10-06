using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Data;
using server.Models;
using server.Models.Dtos;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Telegram.Bot;

namespace server.Services
{
    // Реализация сервиса авторизации и регистрации
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;          // Контекст БД для работы с данными
        private readonly ILogger<AuthService> _logger;  // Логгер для записи информации и ошибок

        private readonly IConfiguration _configuration; // Для чтения настроек токена

        public AuthService(AppDbContext db, ILogger<AuthService> logger, IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
        }

        // Метод для генерации JWT токена
        private string GenerateJwtToken(int userId, string role)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("SecretKey");
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured.");
            }

            var issuer = jwtSettings.GetValue<string>("Issuer");
            var audience = jwtSettings.GetValue<string>("Audience");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim("id", userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Отправка кода подтверждения на телефон
        public async Task<(bool Success, IEnumerable<string>? Errors)> SendVerificationCodeAsync(SendCodeRequest request)
        {
            // Валидация входных данных запроса
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage ?? "Unknown error");
                _logger.LogWarning("Validation failed for SendCodeRequest: {Errors}", string.Join(", ", errors));
                return (false, errors);
            }

            // Ограничение частоты отправки кодов — не больше 55 за последний час
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recentCodes = await _db.VerificationCodes
                .Where(v => v.Phone == request.Phone && v.CreatedAt > oneHourAgo)
                .ToListAsync();

            if (recentCodes.Count >= 55)
            {
                _logger.LogWarning("Too many requests for {Phone}", request.Phone);
                return (false, new[] { "Too many requests" });
            }

            // Генерация случайного 6-значного кода
            var code = new Random().Next(100000, 999999).ToString();

            // Создаем новую запись кода подтверждения
            var verificationCode = new VerificationCode
            {
                Phone = request.Phone,
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            try
            {
                // Удаляем старые неиспользованные коды для этого номера
                var oldCodes = await _db.VerificationCodes
                    .Where(v => v.Phone == request.Phone && !v.IsUsed)
                    .ToListAsync();
                _db.VerificationCodes.RemoveRange(oldCodes);

                // Добавляем новый код и сохраняем изменения
                _db.VerificationCodes.Add(verificationCode);
                await _db.SaveChangesAsync();

                Console.WriteLine($"Verification code for {request.Phone}: {code}");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification code to {Phone}", request.Phone);
                return (false, new[] { "Failed to send code" });
            }
        }

        // Подтверждение регистрации пользователя по коду
        public async Task<(bool Success, string? Message, object? User)> ConfirmRegistrationAsync(ConfirmRegistrationRequest request)
        {
            // Валидация данных запроса
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage ?? "Unknown error");
                _logger.LogWarning("Validation failed for ConfirmRegistrationRequest: {Errors}", string.Join(", ", errors));
                return (false, "Validation failed", null);
            }

            // Проверяем наличие кода подтверждения в базе (неиспользованный)
            var verificationCode = await _db.VerificationCodes
                .FirstOrDefaultAsync(v => v.Phone == request.Phone && v.Code == request.Code && !v.IsUsed);

            if (verificationCode == null)
            {
                return (false, "Invalid code", null);
            }

            // Проверяем срок действия кода
            if (DateTime.UtcNow > verificationCode.ExpiresAt)
            {
                return (false, "Code expired", null);
            }

            // Отмечаем код как использованный
            verificationCode.IsUsed = true;
            await _db.SaveChangesAsync();

            // Проверяем, существует ли уже пользователь с таким телефоном и ролью
            var existingPassenger = await _db.Passengers.FirstOrDefaultAsync(p => p.Phone == request.Phone);
            var existingDriver = await _db.Drivers.FirstOrDefaultAsync(d => d.Phone == request.Phone);

            if (request.Role == "passenger" && existingPassenger != null)
            {
                var token = GenerateJwtToken(existingPassenger.Id, "passenger");
                // Если пассажир найден — это вход
                return (true, "Login successful", new
                {
                    existingPassenger.Id,
                    existingPassenger.Phone,
                    role = "passenger",
                    token
                });
            }
            else if (request.Role == "driver" && existingDriver != null)
            {
                var token = GenerateJwtToken(existingDriver.Id, "driver");
                // Если водитель найден — это вход
                return (true, "Login successful", new
                {
                    existingDriver.Id,
                    existingDriver.Phone,
                    role = "driver",
                    token
                });
            }

            // Если пользователя нет — регистрируем нового
            try
            {
                if (request.Role == "passenger")
                {
                    var passenger = new Passenger { Phone = request.Phone };
                    _db.Passengers.Add(passenger);
                    await _db.SaveChangesAsync();

                    var token = GenerateJwtToken(passenger.Id, "passenger");
                    return (true, "Registration successful", new
                    {
                        passenger.Id,
                        passenger.Phone,
                        role = "passenger",
                        token
                    });
                }
                else if (request.Role == "driver")
                {
                    var driver = new Driver { Phone = request.Phone };
                    _db.Drivers.Add(driver);
                    await _db.SaveChangesAsync();
                    var token = GenerateJwtToken(driver.Id, "driver");
                    return (true, "Registration successful", new
                    {
                        driver.Id,
                        driver.Phone,
                        role = "driver",
                        token
                    });
                }
                else
                {
                    return (false, "Invalid role", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Phone}", request.Phone);
                return (false, "Internal server error", null);
            }
        }
    }
}
