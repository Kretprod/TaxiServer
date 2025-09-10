using System.ComponentModel.DataAnnotations;

namespace server.Models.Dtos
{
    public class SendCodeRequest
    {
        [Required]
        [Phone]
        public required string Phone { get; set; }

        [Required]
        public required string Role { get; set; } // "passenger" или "driver"
    }

    public class ConfirmRegistrationRequest
    {
        [Required]
        [Phone]
        public required string Phone { get; set; }

        [Required]
        public required string Code { get; set; }

        [Required]
        public required string Role { get; set; }
    }
}
