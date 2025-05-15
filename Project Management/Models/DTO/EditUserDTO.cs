using SendGrid.Helpers.Mail;
using System.ComponentModel.DataAnnotations;

namespace Project_Management.Models.DTO
{
    public class EditUserDTO
    {
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string Bio { get; set; }
        [EmailAddress]
        public string Email  { get; set; }
    }
}
