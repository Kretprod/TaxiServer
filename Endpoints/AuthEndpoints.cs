using server.Models.Dtos;
using server.Services;

namespace server.Endpoints
{
    public static class AuthEndpoints
    {
        // Регистрация маршрутов для авторизации и регистрации пользователей
        public static void MapAuthEndpoints(this WebApplication app)
        {
            // Отправка кода подтверждения на телефон
            app.MapPost("/api/auth/send-code", async (SendCodeRequest request, IAuthService authService) =>
            {
                var (success, errors) = await authService.SendVerificationCodeAsync(request);
                if (!success)
                    return Results.BadRequest(new { Errors = errors });
                return Results.Ok(new { msg = "Код отправлен" });
            });

            // Подтверждение регистрации по коду
            app.MapPost("/api/auth/confirm-registration", async (ConfirmRegistrationRequest request, IAuthService authService) =>
            {
                var (success, message, user) = await authService.ConfirmRegistrationAsync(request);
                if (!success)
                    return Results.BadRequest(new { msg = message });
                return Results.Ok(new { msg = message, user });
            });
        }
    }
}
