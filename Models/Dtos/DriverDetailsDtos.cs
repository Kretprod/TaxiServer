using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace server.Models.Dtos
{
    public class DriverDetailsDto
    {
        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string CarNumber { get; set; } = null!;

        [Required]
        public string DriverLicenseNumber { get; set; } = null!;

        [Required]
        public string CarPhotoUrl { get; set; } = null!;

        [Required]
        public string DriverLicensePhotoUrl { get; set; } = null!;

    }

    public class DriverDetailsDtoFront
    {
        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string CarNumber { get; set; } = null!;

        [Required]
        public string DriverLicenseNumber { get; set; } = null!;

        [Required]
        public DriverStatus Status { get; set; }

    }
}
