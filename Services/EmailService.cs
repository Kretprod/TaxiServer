using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace server.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationCodeAsync(string toEmail, string code)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("TaxiPol", emailSettings["SmtpUser"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Код подтверждения";

            message.Body = new TextPart("plain")
            {
                Text = $"Ваш код подтверждения: {code}"
            };

            using var client = new SmtpClient();

            var portString = emailSettings["SmtpPort"];
            if (string.IsNullOrEmpty(portString) || !int.TryParse(portString, out int port))
            {
                throw new InvalidOperationException("SMTP port is not configured or invalid.");
            }

            await client.ConnectAsync(emailSettings["SmtpHost"], port, true);
            await client.AuthenticateAsync(emailSettings["SmtpUser"], emailSettings["SmtpPass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
