using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MediCare.Services.Interfaces;

namespace MediCare.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string message)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPort = _configuration.GetValue<int>("Email:SmtpPort");
        var senderEmail = _configuration["Email:SenderEmail"];
        var senderName = _configuration["Email:SenderName"];
        var password = _configuration["Email:Password"];

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(senderEmail) ||
            string.IsNullOrWhiteSpace(password) ||
            password == "YourEmailPassword" ||
            password == "your-gmail-app-password")
        {
            _logger.LogError("Email configuration is missing or invalid. Please check the 'Email' section in appsettings.json or provide environment variables (e.g., Email__Password).");
            throw new InvalidOperationException("Email configuration is missing or invalid.");
        }

        var email = new MimeMessage();

        email.From.Add(
            new MailboxAddress(senderName, senderEmail));

        email.To.Add(
            MailboxAddress.Parse(toEmail));

        email.Subject = subject;

        email.Body = new TextPart("plain")
        {
            Text = message
        };

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            smtpHost,
            smtpPort,
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            senderEmail,
            password);

        await smtp.SendAsync(email);

        await smtp.DisconnectAsync(true);

        _logger.LogInformation(
            "Email sent successfully to {Email}.",
            toEmail);
    }
}