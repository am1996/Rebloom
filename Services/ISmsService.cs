using System.Threading.Tasks;

namespace Rebloom.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }
}
