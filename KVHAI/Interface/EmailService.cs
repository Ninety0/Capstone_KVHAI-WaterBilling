using KVHAI.Models;
using System.Net;
using System.Net.Mail;

namespace KVHAI.Interface
{
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmail(EmailDto request)
        {
            try 
            {
                var fromEmail = _config["Gmail:Username"];
                var appPassword = _config["Gmail:AppPassword"];
                
                // Validate configuration
                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(appPassword))
                {
                    throw new InvalidOperationException("Gmail configuration is missing");
                }

                using (var mail = new MailMessage())
                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    // Validate request
                    if (string.IsNullOrEmpty(request.To))
                    {
                        throw new ArgumentException("Recipient email is required");
                    }

                    mail.From = new MailAddress(fromEmail);
                    mail.To.Add(request.To);
                    mail.Subject = request.Subject ?? ""; // Handle null subject
                    mail.Body = request.Body ?? ""; // Handle null body
                    mail.IsBodyHtml = true;
                    
                    client.Port = 587;
                    client.Host = "smtp.gmail.com";
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Credentials = new NetworkCredential(fromEmail, appPassword);
                    
                    try
                    {
                        await client.SendMailAsync(mail);
                    }
                    catch (SmtpException smtpEx)
                    {
                        if (smtpEx.Message.Contains("5.7.0")) // Authentication error
                        {
                            throw new Exception("Email authentication failed. Please check your app password.", smtpEx);
                        }
                        throw;
                    }
                }
            }
            catch (SmtpException ex)
            {
                // Log the error details
                throw new Exception($"SMTP error while sending email: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Log the error details
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }
        
    }
}
