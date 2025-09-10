using System.Text.Json.Serialization;

namespace server.Models
{
    public enum PaymentMethod
    {
        Наличка,
        Перевод
    }

    public enum RideStatus
    {
        Ищет,
        Ожидает,
        Подъезжает,
        Впути
    }

    public class Ride
    {
        public int Id { get; set; }

        [JsonPropertyName("passenger_id")]
        public int PassengerId { get; set; }

        [JsonIgnore]
        public Passenger Passenger { get; set; } = null!;

        [JsonPropertyName("driver_id")]
        public int? DriverId { get; set; }

        [JsonIgnore]
        public Driver? Driver { get; set; }

        [JsonPropertyName("pickup_location")]
        public string PickupLocation { get; set; } = string.Empty;

        [JsonPropertyName("pickup_latitude")]
        public decimal PickupLatitude { get; set; }

        [JsonPropertyName("pickup_longitude")]
        public decimal PickupLongitude { get; set; }

        [JsonPropertyName("dropoff_location")]
        public string DropoffLocation { get; set; } = string.Empty;

        [JsonPropertyName("dropoff_latitude")]
        public decimal DropoffLatitude { get; set; }

        [JsonPropertyName("dropoff_longitude")]
        public decimal DropoffLongitude { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("distance")]
        public decimal Distance { get; set; }

        [JsonPropertyName("payment_method")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RideStatus Status { get; set; }
    }
}
