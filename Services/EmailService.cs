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

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("User Management App", _configuration["Email:From"]));
                message.To.Add(new MailboxAddress(userName, toEmail));
                message.Subject = "Welcome to User Management App!";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <h2>Welcome, {userName}!</h2>
                        <p>Thank you for registering at User Management App.</p>
                        <p>We're excited to have you on board!</p>
                    "
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(
                        _configuration["Email:SmtpServer"],
                        int.Parse(_configuration["Email:Port"] ?? "587"),
                        MailKit.Security.SecureSocketOptions.StartTls
                    );

                    await client.AuthenticateAsync(
                        _configuration["Email:Username"],
                        _configuration["Email:Password"]
                    );

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }
    }
}