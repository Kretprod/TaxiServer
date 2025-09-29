namespace server.Services
{
    // Интерфейс для сервиса расчёта стоимости поездки
    public interface ICena
    {
        Task<decimal> CalculatePriceAsync(decimal distanceKm, bool isNight, bool isBadWeather);
        Task<(bool isNight, bool isBadWeather)> GetConditionsAsync();
    }
}
