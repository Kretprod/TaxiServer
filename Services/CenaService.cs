using server.Data;
using server.Models;
using System.Text.Json;

namespace server.Services
{
    public class CenaService : ICena
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _httpClient;

        public CenaService(IConfiguration configuration, AppDbContext db, HttpClient httpClient)
        {
            _db = db;
            _httpClient = httpClient;
        }

        public async Task<decimal> CalculatePriceAsync(decimal distanceKm, bool isNight, bool isBadWeather)
        {
            var settings = await _db.PricingSettings.FindAsync(1);

            if (settings == null)
            {
                settings = new PricingSettings();
            }

            decimal price = settings.BasePrice + distanceKm * settings.PricePerKm;

            if (isNight)
                price *= settings.NightMultiplier;

            if (isBadWeather)
                price *= settings.BadWeatherMultiplier;

            return Math.Round(price, 1);
        }

        public async Task<(bool isNight, bool isBadWeather)> GetConditionsAsync()
        {
            // Получаем время, температуру и дождь из API
            var (observationTime, tempC, isRaining) = await GetWeatherDataAsync();

            // Проверка ночи: ночь с 22:00 до 06:00 по локальному времени из API
            bool isNight = false;
            if (observationTime.HasValue)
            {
                int hour = observationTime.Value.Hour;
                isNight = hour >= 22 || hour < 6;
            }

            // Проверка плохой погоды: дождь или температура < 10°C
            bool isBadWeather = (isRaining == true) || (tempC.HasValue && tempC.Value < 10);

            return (isNight, isBadWeather);
        }

        private async Task<(DateTime? observationTime, int? tempC, bool? isRaining)> GetWeatherDataAsync()
        {
            try
            {
                var url = "https://wttr.in/53.1428,90.4167?format=j1"; // Координаты Абакана, JSON формат
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return (null, null, null);

                var json = await response.Content.ReadAsStringAsync();
                var weatherData = JsonSerializer.Deserialize<WttrWeatherResponse>(json);

                // Извлекаем время наблюдения (
                DateTime? observationTime = null;
                if (DateTime.TryParse(weatherData?.current_condition?[0]?.localObsDateTime, out DateTime parsedTime))
                {
                    observationTime = parsedTime;
                }

                // Извлекаем текущую температуру (temp_C)
                int? tempC = null;
                if (int.TryParse(weatherData?.current_condition?[0]?.temp_C, out int temp))
                {
                    tempC = temp;
                }

                // Извлекаем наличие дождя (precipMM > 0)
                bool? isRaining = null;
                if (double.TryParse(weatherData?.current_condition?[0]?.precipMM, out double precip))
                {
                    isRaining = precip > 0;
                }

                return (observationTime, tempC, isRaining);
            }
            catch
            {
                // В случае ошибки API, возвращаем null
                return (null, null, null);
            }
        }
    }

    // Модель для ответа wttr.in (упрощённая, только нужные поля)
    public class WttrWeatherResponse
    {
        public List<CurrentCondition>? current_condition { get; set; }
    }

    public class CurrentCondition
    {
        public string localObsDateTime { get; set; } = string.Empty;
        public string temp_C { get; set; } = string.Empty;
        public string precipMM { get; set; } = string.Empty; // Добавлено для проверки дождя
    }
}
