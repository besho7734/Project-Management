using Microsoft.AspNetCore.Identity;

namespace Project_Management.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string bio { get; set; }
    }
}
