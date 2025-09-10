using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
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
            // POST /api/auth/send-code — отправить код подтверждения на телефон
            app.MapPost("/api/auth/send-code", async (SendCodeRequest request, AppDbContext db, SmsService smsService, ILogger<Program> logger) =>
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    logger.LogWarning("Validation failed for SendCodeRequest: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
                    return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
                }

                var oneHourAgo = DateTime.UtcNow.AddHours(-1);
                var recentCodes = await db.VerificationCodes
                    .Where(v => v.Phone == request.Phone && v.CreatedAt > oneHourAgo)
                    .ToListAsync();
                if (recentCodes.Count >= 10)
                {
                    logger.LogWarning("Too many requests for {Phone}", request.Phone);
                    return Results.StatusCode(StatusCodes.Status429TooManyRequests);
                }

                var code = new Random().Next(100000, 999999).ToString();

                var verificationCode = new VerificationCode
                {
                    Phone = request.Phone,
                    Code = code,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10)
                };



                try
                {
                    var oldCodes = await db.VerificationCodes
                        .Where(v => v.Phone == request.Phone && !v.IsUsed)
                        .ToListAsync();
                    db.VerificationCodes.RemoveRange(oldCodes);

                    db.VerificationCodes.Add(verificationCode);
                    await db.SaveChangesAsync();

                    // await smsService.SendVerificationCodeAsync(request.Phone, code);
                    Console.WriteLine($"Код верификации отправлен на {verificationCode.Phone}: {code} (для теста)");
                    logger.LogInformation("Verification code sent to {Phone}", request.Phone);
                    return Results.Ok(new { msg = "Код отправлен" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending code to {Phone}", request.Phone);
                    return Results.Problem("Failed to send code");
                }
            });

            // POST /api/auth/confirm-registration — подтвердить регистрацию по коду
            app.MapPost("/api/auth/confirm-registration", async (ConfirmRegistrationRequest request, AppDbContext db, ILogger<Program> logger) =>
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(request);
                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    logger.LogWarning("Validation failed for ConfirmRegistrationRequest: {Errors}", string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
                    return Results.BadRequest(new { Errors = validationResults.Select(v => v.ErrorMessage) });
                }

                var verificationCode = await db.VerificationCodes
                    .FirstOrDefaultAsync(v => v.Phone == request.Phone && v.Code == request.Code && !v.IsUsed);

                if (verificationCode == null)
                {
                    return Results.BadRequest(new { msg = "Неверный код" });
                }

                if (DateTime.UtcNow > verificationCode.ExpiresAt)
                {
                    return Results.BadRequest(new { msg = "Срок действия кода истёк" });
                }

                verificationCode.IsUsed = true;
                await db.SaveChangesAsync();

                var existingPassenger = await db.Passengers.FirstOrDefaultAsync(p => p.Phone == request.Phone);
                var existingDriver = await db.Drivers.FirstOrDefaultAsync(d => d.Phone == request.Phone);

                if (request.Role == "passenger" && existingPassenger != null)
                {
                    return Results.Ok(new
                    {
                        msg = "Вход прошёл успешно",
                        user = new
                        {
                            existingPassenger.Id,
                            existingPassenger.Phone,
                            role = "passenger"  // добавляем поле role вручную
                        }
                    });
                }
                else if (request.Role == "driver" && existingDriver != null)
                {
                    return Results.Ok(new
                    {
                        msg = "Вход прошёл успешно",
                        user = new
                        {
                            existingDriver.Id,
                            existingDriver.Phone,
                            role = "driver"  // добавляем поле role вручную
                        }
                    });
                }

                try
                {
                    if (request.Role == "passenger")
                    {
                        var passenger = new Passenger
                        {
                            Phone = request.Phone,
                        };
                        db.Passengers.Add(passenger);
                        await db.SaveChangesAsync();
                        return Results.Ok(new
                        {
                            msg = "Регистрация прошла успешно",
                            user = new
                            {
                                passenger.Id,
                                passenger.Phone,
                                role = "passenger"  // добавляем поле role вручную
                            }
                        });
                    }
                    else if (request.Role == "driver")
                    {
                        var driver = new Driver
                        {
                            Phone = request.Phone,
                        };
                        db.Drivers.Add(driver);
                        await db.SaveChangesAsync();
                        return Results.Ok(new
                        {
                            msg = "Регистрация прошла успешно",
                            user = new
                            {
                                driver.Id,
                                driver.Phone,
                                role = "driver"  // добавляем поле role вручную
                            }
                        });
                    }
                    else
                    {
                        return Results.BadRequest(new { msg = "Неверная роль" });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при регистрации пользователя {Phone}", request.Phone);
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            });
        }
    }
}
