using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    public abstract class User
    {
        [Key]  // если Id — первичный ключ
        public int Id { get; set; }  // Изменено с id на Id

        [Required]
        [Phone]  // Добавьте атрибут для телефона
        public string Phone { get; set; } = string.Empty;  // Изменено с phone на Phone, убрано required (если .NET <7)
    }
}
