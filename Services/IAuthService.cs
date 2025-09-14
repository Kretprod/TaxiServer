using server.Models.Dtos;
using server.Models;

namespace server.Services
{
    // Интерфейс для сервиса авторизации — описывает контракт для реализации
    public interface IAuthService
    {
        // Метод отправки кода подтверждения — возвращает успех и список ошибок (если есть)
        Task<(bool Success, IEnumerable<string>? Errors)> SendVerificationCodeAsync(SendCodeRequest request);

        // Метод подтверждения регистрации — возвращает успех, сообщение и данные пользователя
        Task<(bool Success, string? Message, object? User)> ConfirmRegistrationAsync(ConfirmRegistrationRequest request);
    }
}
