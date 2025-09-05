namespace server.Models.Dtos
{
    public class SendCodeRequest
    {
        public required string Email { get; set; }
        public required string Role { get; set; } // "passenger" или "driver"
    }

    public class ConfirmRegistrationRequest
    {
        public required string Email { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Phone { get; set; }
        public required string Role { get; set; }
    }
}
