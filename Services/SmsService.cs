using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Rebloom.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _config;

        public SmsService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            var sid = _config.GetValue<string>("Twilio:AccountSid");
            var token = _config.GetValue<string>("Twilio:AuthToken");
            var from = _config.GetValue<string>("Twilio:FromNumber");

            if (string.IsNullOrWhiteSpace(sid) || sid.Contains("YOUR_TWILIO") || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(from))
            {
                System.Console.WriteLine($"[SmsService] Dry-run SMS to {phoneNumber}: {message}");
                return;
            }

            TwilioClient.Init(sid, token);
            var msg = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(from),
                to: new PhoneNumber(phoneNumber)
            );

            System.Console.WriteLine($"[SmsService] Sent SMS SID: {msg.Sid}");
        }
    }
}
