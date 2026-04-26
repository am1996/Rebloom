using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Threading.Tasks;

namespace Rebloom.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string username, string token)
        {
            var smtp = _config.GetSection("Smtp");
            var host = smtp.GetValue<string>("Host");
            var port = smtp.GetValue<int>("Port");
            var user = smtp.GetValue<string>("User");
            var pass = smtp.GetValue<string>("Pass");
            var from = smtp.GetValue<string>("From");
            var useSsl = smtp.GetValue<bool>("UseSsl");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Verify your account";

            var verifyUrl = $"https://localhost:7001/api/user/verify?token={token}";
            message.Body = new TextPart("plain")
            {
                Text = $"Hello {username},\n\nPlease verify your account by visiting:\n{verifyUrl}\n\nIf you didn't create an account, ignore this email."
            };

            if (string.IsNullOrEmpty(host) || host == "smtp.example.com")
            {
                // No SMTP configured; write to console as fallback
                System.Console.WriteLine("[EmailService] Verification email (dry-run) to: " + toEmail);
                System.Console.WriteLine("[EmailService] Verify URL: " + verifyUrl);
                return;
            }

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, useSsl);
            if (!string.IsNullOrEmpty(user))
            {
                await client.AuthenticateAsync(user, pass);
            }
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
