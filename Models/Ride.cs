using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required]
        public int PassengerId { get; set; }

        [Required]
        public required Passenger Passenger { get; set; }

        public int? DriverId { get; set; }
        public Driver? Driver { get; set; }

        [Required]
        public required string PickupLocation { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,7)")]
        public decimal PickupLatitude { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,7)")]
        public decimal PickupLongitude { get; set; }

        [Required]
        public required string DropoffLocation { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,7)")]
        public decimal DropoffLatitude { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,7)")]
        public decimal DropoffLongitude { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Distance { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public RideStatus Status { get; set; } = RideStatus.Ищет;
    }
}
