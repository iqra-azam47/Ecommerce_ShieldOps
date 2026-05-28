using System.Diagnostics;
namespace ShieldOps.Services
{
    public class EmailMockService : IEmailMockService
    {
        public void SendVerificationToken(string email, string token)
        {
            // This outputs the token directly to the Visual Studio Output window and the console terminal screen
            string emailTemplate = $"""
            
            ===================================================================
            SHIELDOPS MOCK SMTP EMAIL SERVICE
            To: {email}
            Subject: Secure Your ShieldOps Account - 6-Digit Verification Code
            
            Your security verification token is: {token}
            This token will expire in 15 minutes.
            ===================================================================
            """;

            Debug.WriteLine(emailTemplate);
            Console.WriteLine(emailTemplate);
        }
    }
}