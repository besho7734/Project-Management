using System.ComponentModel.DataAnnotations;

namespace Project_Management.Models.DTO
{
    public class LoginRequestDTO
    {
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
