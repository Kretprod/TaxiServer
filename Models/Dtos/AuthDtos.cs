using System.ComponentModel.DataAnnotations;

namespace server.Models.Dtos
{
    public class SendCodeRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Role { get; set; } // "passenger" или "driver"
    }

    public class ConfirmRegistrationRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Code { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public required string Phone { get; set; }

        [Required]
        public required string Role { get; set; }
    }
}
