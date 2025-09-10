using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System;

namespace server.Services
{
    public class SmsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public SmsService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task SendVerificationCodeAsync(string toPhone, string code)
        {
            var smsSettings = _configuration.GetSection("SmsSettings");
            var apiId = smsSettings["SmsRuApiId"];  // Ваш API-ключ от SMS.ru

            // Формируем сообщение
            var message = $"Ваш код подтверждения: {code}";
            if (string.IsNullOrEmpty(apiId))
            {
                throw new Exception("API ID не найден в конфигурации. Проверьте appsettings.json.");
            }

            // Убираем + из номера, если есть (SMS.ru ожидает формат без +)
            toPhone = toPhone.TrimStart('+');

            // URL для отправки SMS (с JSON-ответом)
            var url = $"https://sms.ru/sms/send?api_id={apiId}&to={toPhone}&msg={Uri.EscapeDataString(message)}&json=1";

            try
            {
                // Отправляем запрос
                var response = await _httpClient.GetAsync(url);

                // Проверяем статус ответа
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP error: {response.StatusCode} - {response.ReasonPhrase}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"SMS.ru response: {responseContent}");  // Логируем для отладки

                // Парсим JSON-ответ
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                // Безопасно проверяем "status"
                if (!root.TryGetProperty("status", out var statusProp) || statusProp.GetString() != "OK")
                {
                    var error = root.TryGetProperty("status_text", out var statusTextProp)
                        ? statusTextProp.GetString()
                        : "Unknown error";
                    throw new Exception($"SMS sending failed: {error}");
                }

                // Безопасно получаем "sms_id" (если есть)
                var smsId = root.TryGetProperty("sms_id", out var smsIdProp)
                    ? smsIdProp.GetString()
                    : "N/A";

                // Логируем успех
                Console.WriteLine($"SMS sent successfully to {toPhone}, ID: {smsId}");
            }
            catch (JsonException jsonEx)
            {
                throw new Exception($"Failed to parse SMS response: {jsonEx.Message}. Raw response: ");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send SMS: {ex.Message}");
            }
        }
    }
}
