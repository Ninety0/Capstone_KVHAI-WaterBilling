using KVHAI.Interface;
using KVHAI.Models;
using Microsoft.AspNetCore.Html;
using System.Security.Cryptography;

namespace KVHAI.Repository
{
    public class LoginRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly IEmailSender _emailService;

        public LoginRepository(DBConnect dbconn, IEmailSender emailService)
        {
            _dbConnect = dbconn;
            _emailService = emailService;
        }

        public async Task<List<CodeGenerator>> GenerateVerificationCode(int length = 4)
        {
            var codeList = new List<CodeGenerator>();
            // Buffer to hold random bytes
            byte[] randomNumber = new byte[4]; // 4 bytes for generating the random number
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            // Convert the random number to an integer
            int randomValue = BitConverter.ToInt32(randomNumber, 0);

            // Ensure the number is positive and within the required range
            randomValue = Math.Abs(randomValue % (int)Math.Pow(10, length));

            // Set expiration time to 5 minutes from now
            DateTime expirationTime = DateTime.Now.AddMinutes(3);

            var code = new CodeGenerator
            {
                Code = randomValue.ToString($"D{length}"),
                Expiration = expirationTime
            };

            codeList.Add(code);

            // Return the verification code as a string, padded with leading zeroes if necessary
            return codeList;
        }

        public async Task SendEmail(EmailDto request, string code)
        {
            //request.To = "dorojavince@gmail.com";
            request.Subject = "KVHAI verification code";
            request.Body = VerificationBody(request.To, code).ToString();
            await _emailService.SendEmail(request);
        }

        public HtmlString VerificationBody(string email, string code)
        {
            string body = "<div class=\"border border-1\"><div class=\"text-center\" style=\"background-color: #052572; padding: 20px; color: white;\">KVHAI Verification Code</div><div class=\"verification_body p-5\">";
            body += $@"
                Hi {email},
                <br/>
                <br/>
                CODE VERIFICAITON:
                <h3>{code}</h3>
                Please note that the email code is valid for 3 minutes. If the email code expires, you will need to request a new one.
                </div>
                </div>
            ";

            return new HtmlString(body);
        }
    }
}
