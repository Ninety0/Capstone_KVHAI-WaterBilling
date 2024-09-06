namespace KVHAI.CustomClass
{
    public class EmailSender//: IEmailSender
    {
        //private readonly SmtpSettings _smtpSettings;

        //public EmailService(IOptions<SmtpSettings> smtpSettings)
        //{
        //    _smtpSettings = smtpSettings.Value;
        //}

        //public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        //{
        //    using (var client = new SmtpClient(_smtpSettings.SmtpServer, _smtpSettings.Port))
        //    {
        //        client.UseDefaultCredentials = false;
        //        client.Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password);
        //        client.EnableSsl = true;

        //        var mailMessage = new MailMessage
        //        {
        //            From = new MailAddress(_smtpSettings.UserName),
        //            Subject = subject,
        //            Body = htmlMessage,
        //            IsBodyHtml = true
        //        };
        //        mailMessage.To.Add(email);

        //        await client.SendMailAsync(mailMessage);
        //    }
        //}
    }
}
