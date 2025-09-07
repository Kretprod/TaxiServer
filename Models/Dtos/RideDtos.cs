using System.ComponentModel.DataAnnotations;

namespace server.Models.Dtos
{
    public class RideCreateDto
    {
        [Required]
        public int PassengerId { get; set; }

        public int? DriverId { get; set; }

        [Required]
        public required string PickupLocation { get; set; }

        [Required]
        public decimal PickupLatitude { get; set; }

        [Required]
        public decimal PickupLongitude { get; set; }

        [Required]
        public required string DropoffLocation { get; set; }

        [Required]
        public decimal DropoffLatitude { get; set; }

        [Required]
        public decimal DropoffLongitude { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public decimal Distance { get; set; }

        [Required]
        public required string PaymentMethod { get; set; }
    }

    public class AcceptRideRequest
    {
        [Required]
        public int RideId { get; set; }

        [Required]
        public int DriverId { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public RideStatus NewStatus { get; set; }
    }
}
