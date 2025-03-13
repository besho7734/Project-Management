using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Project_Management
{
    public class EmailSender : IEmailSender
    {
        public string SendGridSecret { get; set; }
        public EmailSender(IConfiguration configuration)
        {
            SendGridSecret = configuration.GetValue<string>("SendGrid:Secret");
        }
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SendGridClient(SendGridSecret);
            var from = new EmailAddress("besho7734@gmail.com", "Project Management");
            var to = new EmailAddress(email);
            var messege = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);
            return client.SendEmailAsync(messege);
        }
    }
}
