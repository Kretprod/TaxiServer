using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace server.Models
{
    public enum DriverStatus
    {
        Активен,
        Неактивен,
        НаПроверке
    }
    public class DriverDetails
    {
        [Key, ForeignKey("Driver")]
        public int DriverId { get; set; }

        public Driver Driver { get; set; } = null!;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string CarNumber { get; set; } = string.Empty;

        [Required]
        public string DriverLicenseNumber { get; set; } = string.Empty;

        [Required]
        public string CarPhotoUrl { get; set; } = string.Empty;

        [Required]
        public string DriverLicensePhotoUrl { get; set; } = string.Empty;

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DriverStatus Status { get; set; }
    }
}
