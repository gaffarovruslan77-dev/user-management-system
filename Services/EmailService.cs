using MailKit.Net.Smtp;
using MimeKit;

namespace UserManagementApp.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string userName, string verificationUrl)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("User Management App", _configuration["EmailSettings:From"]));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Verify Your Email - User Management App";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background: #2563eb; color: white; padding: 20px; text-align: center; }}
                            .content {{ background: #f9fafb; padding: 30px; }}
                            .button {{ display: inline-block; background: #2563eb; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                            .footer {{ text-align: center; padding: 20px; color: #6b7280; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>THE APP</h1>
                            </div>
                            <div class='content'>
                                <h2>Welcome, {userName}!</h2>
                                <p>Thank you for registering with User Management App.</p>
                                <p>Please verify your email address by clicking the button below:</p>
                                <a href='{verificationUrl}' class='button'>Verify Email</a>
                                <p>Or copy and paste this link into your browser:</p>
                                <p style='word-break: break-all; color: #2563eb;'>{verificationUrl}</p>
                                <p>If you didn't create this account, please ignore this email.</p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2024 User Management App. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(_configuration["EmailSettings:Port"]),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:Password"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}