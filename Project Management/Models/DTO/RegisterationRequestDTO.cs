using System.ComponentModel.DataAnnotations;

namespace Project_Management.Models.DTO
{
    public class RegisterationRequestDTO
    { 
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }
        public string Bio { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? Image { get; set; }

        //public string Role { get; set; }
    }
}
