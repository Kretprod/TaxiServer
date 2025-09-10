using System;
using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    public class VerificationCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Phone]  // Исправлено с [EmailAddress] на [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;
    }
}
