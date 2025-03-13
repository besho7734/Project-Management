using System.ComponentModel.DataAnnotations;

namespace Project_Management.Models.DTO
{
    public class ResetPasswordDTO
    {
        [EmailAddress]
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
