using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    public class TelegramUser
    {
        [Key]
        public int Id { get; set; }

        public long ChatId { get; set; }

        [Required]
        public required string PhoneNumber { get; set; }
    }
}
