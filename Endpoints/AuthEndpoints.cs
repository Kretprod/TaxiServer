// AuthEndpoints.cs
using System.ComponentModel.DataAnnotations; // для ValidationResult, ValidationContext
using Microsoft.EntityFrameworkCore;        // для AnyAsync и других EF Core методов
using server.Models.Dtos;
using server.Services;
using server.Data;
using server.Models;
namespace server.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            // POST /api/auth/send-code — отправить код подтверждения на email
            app.MapPost("/api/auth/send-code", async (SendCodeRequest request, AppDbContext db, EmailService emailService, ILogger<Program> logger) =>
            {
                Console.WriteLine("asdsa");
                // Валидация запроса
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    logger.LogWarning("Validation failed for SendCodeRequest: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
                    return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
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
                    return Results.Ok(new { msg = "Код отправлен" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending code to {Email}", request.Email);
                    return Results.Problem("Failed to send code");
                }
            });

            // POST /api/auth/confirm — подтвердить регистрацию по коду
            app.MapPost("/api/auth/confirm-registration", async (ConfirmRegistrationRequest request, AppDbContext db, ILogger<Program> logger) =>
            {
                // Валидация входных данных
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    logger.LogWarning("Validation failed for ConfirmRegistrationRequest: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
                    return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
                }

                // Ищем код в базе, который совпадает с email и кодом, и не использован
                var verificationCode = await db.VerificationCodes
                    .FirstOrDefaultAsync(v => v.Email == request.Email && v.Code == request.Code && !v.IsUsed);

                if (verificationCode == null)
                {
                    return Results.BadRequest(new { msg = "Неверный код" });
                }

                // Проверяем срок действия кода
                if (DateTime.UtcNow > verificationCode.ExpiresAt)
                {
                    return Results.BadRequest(new { msg = "Срок действия кода истёк" });
                }

                // Помечаем код как использованный
                verificationCode.IsUsed = true;
                await db.SaveChangesAsync();

                // Проверяем, есть ли уже пользователь с таким email (пассажир или водитель)
                var existingPassenger = await db.Passengers.FirstOrDefaultAsync(p => p.Email == request.Email);
                var existingDriver = await db.Drivers.FirstOrDefaultAsync(d => d.Email == request.Email);
                if (request.Role == "passenger" && existingPassenger != null)
                {
                    return Results.Ok(new { msg = "Вход прошёл успешно", user = existingPassenger });
                }
                else if (request.Role == "driver" && existingDriver != null)
                {
                    return Results.Ok(new { msg = "Вход прошёл успешно", user = existingDriver });
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
                            Phone = request.Phone,
                        };
                        db.Passengers.Add(passenger);
                        await db.SaveChangesAsync();
                        return Results.Ok(new { msg = "Регестрация прошла успешно", user = passenger });
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
                        return Results.Ok(new { msg = "Регестрация прошла успешно", user = driver });
                    }
                    else
                    {
                        return Results.BadRequest(new { msg = "Неверная роль" });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при регистрации пользователя {Email}", request.Email);
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });
        }
    }
}
