using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace server.Models.Dtos
{
    public class CenaDtos
    {
        [Required]
        public decimal distanceKm { get; set; }
    }

    public class CenaResponseDto
    {
        public decimal Price { get; set; }
        public bool IsNight { get; set; }
        public bool IsBadWeather { get; set; }
    }
}