using KVHAI.Models;

namespace KVHAI.Interface
{
    public interface IEmailSender
    {
        //Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendEmail(EmailDto request);
    }
}
