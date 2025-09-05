using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    public abstract class User
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Phone { get; set; }
    }
}
