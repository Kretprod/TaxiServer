namespace server.Models.Dtos
{
    public class RideCreateDto
    {
        public int PassengerId { get; set; }
        public int? DriverId { get; set; }

        public required string PickupLocation { get; set; }
        public decimal PickupLatitude { get; set; }
        public decimal PickupLongitude { get; set; }

        public required string DropoffLocation { get; set; }
        public decimal DropoffLatitude { get; set; }
        public decimal DropoffLongitude { get; set; }

        public decimal Price { get; set; }
        public decimal Distance { get; set; }

        public required string PaymentMethod { get; set; }
        public required string Status { get; set; }
    }
}
