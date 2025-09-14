using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace server.Models.Dtos
{
    public class RideCreateDto
    {

        public int? DriverId { get; set; }

        [Required]
        [JsonPropertyName("pickup_location")]
        public required string PickupLocation { get; set; }

        [Required]
        [JsonPropertyName("pickup_latitude")]
        public decimal PickupLatitude { get; set; }

        [Required]
        [JsonPropertyName("pickup_longitude")]
        public decimal PickupLongitude { get; set; }

        [Required]
        [JsonPropertyName("dropoff_location")]
        public required string DropoffLocation { get; set; }

        [Required]
        [JsonPropertyName("dropoff_latitude")]
        public decimal DropoffLatitude { get; set; }

        [Required]
        [JsonPropertyName("dropoff_longitude")]
        public decimal DropoffLongitude { get; set; }

        [Required]
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [Required]
        [JsonPropertyName("distance")]
        public decimal Distance { get; set; }

        [Required]
        [JsonPropertyName("payment_method")]
        public required string PaymentMethod { get; set; }
    }

    public class AcceptOrderRequest
    {
        [JsonPropertyName("driver_id")] // если используете System.Text.Json
        public int DriverId { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [Required]
        public RideStatus NewStatus { get; set; }
    }
    public class PriceUpdateRequest
    {
        public decimal Amount { get; set; }
    }
}
