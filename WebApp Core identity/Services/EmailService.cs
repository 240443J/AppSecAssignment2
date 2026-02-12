using System.Net;
using System.Net.Mail;

namespace WebApp_Core_Identity.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generic method to send emails - used for 2FA OTP and other notifications
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var fromEmail = _configuration["EmailSettings:SenderEmail"];
                var fromName = _configuration["EmailSettings:SenderName"] ?? "Fresh Farm Market";
                var appPassword = _configuration["EmailSettings:AppPassword"];

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(fromEmail, appPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                throw;
            }
        }

        /// <summary>
        /// Sends a formatted password reset email with HTML template
        /// </summary>
        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var fromEmail = _configuration["EmailSettings:SenderEmail"];
                var fromName = _configuration["EmailSettings:SenderName"] ?? "Fresh Farm Market";
                var appPassword = _configuration["EmailSettings:AppPassword"];

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(appPassword))
                {
                    _logger.LogError("Email configuration is missing. Check appsettings.json");
                    throw new InvalidOperationException("Email configuration is incomplete");
                }

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(fromEmail, appPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = "?? Reset Your Password - Fresh Farm Market",
                    Body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
 .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 15px 0; }}
    </style>
</head>
<body>
  <div class='container'>
        <div class='header'>
  <h1>?? Fresh Farm Market</h1>
        </div>
        <div class='content'>
    <h2>Password Reset Request</h2>
            <p>Hello,</p>
       <p>We received a request to reset your password for your Fresh Farm Market account.</p>
 <p>Click the button below to reset your password:</p>
            <p style='text-align: center;'>
  <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
    <p>Or copy and paste this link into your browser:</p>
  <p style='word-break: break-all; background-color: #f0f0f0; padding: 10px; border-radius: 3px;'>
     {resetLink}
          </p>
      <div class='warning'>
    <strong>?? Important:</strong>
       <ul>
             <li>This link will expire in <strong>30 minutes</strong></li>
<li>This link can only be used once</li>
               <li>If you didn't request this, please ignore this email</li>
       </ul>
  </div>
     <p>For your security, we recommend:</p>
            <ul>
        <li>Using a strong password (min 12 characters)</li>
           <li>Including uppercase, lowercase, numbers, and special characters</li>
     <li>Not reusing your last 2 passwords</li>
            </ul>
        </div>
    <div class='footer'>
       <p>Best regards,<br/>Fresh Farm Market Team</p>
  <p style='font-size: 11px; color: #999;'>
        This is an automated email. Please do not reply to this message.
 </p>
        </div>
    </div>
</body>
</html>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Password reset email sent successfully to {Email}", toEmail);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending password reset email to {Email}", toEmail);
                throw new InvalidOperationException("Failed to send email. Please check your email settings.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
                throw;
            }
        }
    }
}
