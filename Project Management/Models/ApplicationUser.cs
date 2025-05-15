using Microsoft.AspNetCore.Identity;

namespace Project_Management.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string bio { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageLocalPath { get; set; }
    }
}
