using System;
using System.Text.Json.Serialization;

namespace server.Models
{
    public class RideHistory
    {
        public int Id { get; set; } // PK

        public int PassengerId { get; set; }
        public int? DriverId { get; set; }

        public string PickupLocation { get; set; } = string.Empty;
        public decimal PickupLatitude { get; set; }
        public decimal PickupLongitude { get; set; }

        public string DropoffLocation { get; set; } = string.Empty;
        public decimal DropoffLatitude { get; set; }
        public decimal DropoffLongitude { get; set; }

        public decimal Price { get; set; }
        public decimal Distance { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMethod PaymentMethod { get; set; }

        public DateTime CompletedAt { get; set; } // Время завершения поездки
    }
}
