using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using Project_Management.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Project_Management
{
    public class EmailSender : IEmailService
    {
        //public string SendGridSecret { get; set; }
        private readonly EmailSettings _emailSettings;
        public EmailSender(/*IConfiguration configuration,*/IOptions<EmailSettings> options)
        {
            _emailSettings = options.Value;
            //SendGridSecret = configuration.GetValue<string>("SendGrid:Secret");
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var Email = new MimeMessage();
            Email.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.Email));
            Email.To.Add(MailboxAddress.Parse(email));
            Email.Subject = subject;
            var builder = new BodyBuilder();
            builder.HtmlBody = htmlMessage;
            Email.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient();
            smtp.Connect(_emailSettings.Host, _emailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            smtp.Authenticate(_emailSettings.Email, _emailSettings.Password);
            await smtp.SendAsync(Email);
            smtp.Disconnect(true);



            //var client = new SendGridClient(SendGridSecret);
            //var from = new EmailAddress("besho7734@gmail.com", "Project Management");
            //var to = new EmailAddress(email);
            //var messege = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);
            //return client.SendEmailAsync(messege);
        }
    }
}
