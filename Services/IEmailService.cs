using System.Threading.Tasks;

namespace Rebloom.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string username, string token);
    }
}
