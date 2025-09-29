using Microsoft.AspNetCore.Authorization;
using server.Models.Dtos;
using server.Services;

namespace server.Endpoints
{
    public static class AdminnEndpoints
    {
        public static void MapAdminnEndpoints(this WebApplication app)
        {
            // API входа (уже есть)
            app.MapPost("/api/auth/login", async (LoginRequestDto request, IAdminn adminService) =>
            {
                try
                {
                    var response = await adminService.LoginAsync(request);
                    if (response == null)
                        return Results.Unauthorized();
                    return Results.Ok(response);
                }
                catch (Exception)
                {
                    return Results.Problem("Внутренняя ошибка сервера");
                }
            }).AllowAnonymous();

            // Новый эндпоинт: Получить список водителей на проверке (только для админов)
            app.MapGet("/api/drivers/pending", async (IAdminn adminService) =>
            {
                try
                {
                    var drivers = await adminService.GetPendingDriversAsync();
                    return Results.Ok(drivers);
                }
                catch (Exception)
                {
                    return Results.Problem("Ошибка при получении списка водителей");
                }
            }).RequireAuthorization();  // Требует JWT-токен


            // Новый эндпоинт: Получить список активных водителей (только для админов)
            app.MapGet("/api/drivers/active", async (IAdminn adminService) =>
            {
                try
                {
                    var drivers = await adminService.GetActiveDriversAsync();
                    return Results.Ok(drivers);
                }
                catch (Exception)
                {
                    return Results.Problem("Ошибка при получении списка активных водителей");
                }
            }).RequireAuthorization();  // Требует JWT-токен

            // Новый эндпоинт: Обновить статус водителя (только для админов)
            app.MapPut("/api/drivers/{id}/status", async (int id, StatusUpdateDto request, IAdminn adminService) =>
            {
                try
                {
                    await adminService.UpdateDriverStatusAsync(id, request.Status);
                    return Results.Ok(new { Message = "Статус обновлён" });
                }
                catch (Exception)
                {
                    return Results.Problem("Ошибка при обновлении статуса");
                }
            }).RequireAuthorization();  // Требует JWT-токен

            // Новый эндпоинт: Получить текущие настройки прайса (только для админов)
            app.MapGet("/api/pricing", async (IAdminn adminService) =>
            {
                try
                {
                    var pricing = await adminService.GetPricingSettingsAsync();
                    return Results.Ok(pricing);
                }
                catch (Exception)
                {
                    return Results.Problem("Ошибка при получении настроек прайса");
                }
            }).RequireAuthorization();

            // Новый эндпоинт: Обновить настройки прайса (только для админов)
            app.MapPut("/api/pricing", async (UpdatePricingSettingsDto request, IAdminn adminService) =>
            {
                try
                {
                    await adminService.UpdatePricingSettingsAsync(request);
                    return Results.Ok(new { Message = "Настройки прайса обновлены" });
                }
                catch (Exception)
                {
                    return Results.Problem("Ошибка при обновлении настроек прайса");
                }
            }).RequireAuthorization();

            // Новый эндпоинт: Получить текущие условия погоды (ночь и плохая погода) (только для админов)
            app.MapGet("/api/weather/conditions", async (ICena cenaService) =>
            {
                try
                {
                    var (isNight, isBadWeather) = await cenaService.GetConditionsAsync();
                    return Results.Ok(new { isNight = isNight, isBadWeather = isBadWeather });
                }
                catch (Exception)
                {
                    return Results.Problem("Ошибка при получении условий погоды");
                }
            }).RequireAuthorization();  // Требует JWT-токен


        }
    }
}
