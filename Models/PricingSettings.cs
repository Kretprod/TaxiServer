namespace server.Models
{
    public class PricingSettings
    {
        public int Id { get; set; } = 1; // фиксированный Id

        public decimal BasePrice { get; set; } = 50m;

        public decimal PricePerKm { get; set; } = 20m;

        public decimal NightMultiplier { get; set; } = 1.2m;

        public decimal BadWeatherMultiplier { get; set; } = 1.3m;
    }
}
