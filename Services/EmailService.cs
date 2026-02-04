using Resend;
using Microsoft.Extensions.Configuration;

namespace UserManagementApp.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IResend _resend;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IResend resend)
    {
        _configuration = configuration;
        _logger = logger;
        _resend = resend;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string userName, string verificationUrl)
    {
        try
        {
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #0066cc;'>Welcome to User Management App!</h2>
                        <p>Hello <strong>{userName}</strong>,</p>
                        <p>Thank you for registering! Your account has been created with <strong>Unverified</strong> status.</p>
                        <p>You can start using the platform immediately, but we recommend verifying your email address.</p>
                        <p>Click the button below to verify your email and change your status to <strong>Active</strong>:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{verificationUrl}' 
                               style='background-color: #0066cc; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Verify Email
                            </a>
                        </div>
                        <p style='color: #666; font-size: 14px;'>
                            If the button doesn't work, copy and paste this link into your browser:<br>
                            <a href='{verificationUrl}'>{verificationUrl}</a>
                        </p>
                        <p style='color: #666; font-size: 14px;'>
                            This link will expire in 7 days.
                        </p>
                        <hr style='border: 1px solid #eee; margin: 30px 0;'>
                        <p style='color: #999; font-size: 12px;'>
                            If you didn't create this account, please ignore this email.
                        </p>
                    </div>
                </body>
                </html>";

            var message = new EmailMessage();
            message.From = _configuration["Email:FromAddress"] ?? "noreply@yourdomain.com";
            message.To.Add(toEmail);
            message.Subject = "Verify Your Email - User Management App";
            message.HtmlBody = htmlContent;

            var response = await _resend.EmailSendAsync(message);
            _logger.LogInformation($"✅ Verification email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error sending verification email to {toEmail}: {ex.Message}");
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl)
    {
        try
        {
            var htmlContent = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #cc0000;'>Password Reset Request</h2>
                        <p>Hello <strong>{userName}</strong>,</p>
                        <p>We received a request to reset your password.</p>
                        <p>Click the button below to reset your password:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetUrl}' 
                               style='background-color: #cc0000; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Reset Password
                            </a>
                        </div>
                        <p style='color: #666; font-size: 14px;'>
                            If the button doesn't work, copy and paste this link into your browser:<br>
                            <a href='{resetUrl}'>{resetUrl}</a>
                        </p>
                        <p style='color: #666; font-size: 14px;'>
                            This link will expire in 1 hour.
                        </p>
                        <hr style='border: 1px solid #eee; margin: 30px 0;'>
                        <p style='color: #999; font-size: 12px;'>
                            If you didn't request a password reset, please ignore this email. Your password will remain unchanged.
                        </p>
                    </div>
                </body>
                </html>";

            var message = new EmailMessage();
            message.From = _configuration["Email:FromAddress"] ?? "noreply@yourdomain.com";
            message.To.Add(toEmail);
            message.Subject = "Password Reset Request - User Management App";
            message.HtmlBody = htmlContent;

            var response = await _resend.EmailSendAsync(message);
            _logger.LogInformation($"✅ Password reset email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error sending password reset email to {toEmail}: {ex.Message}");
            throw;
        }
    }
}