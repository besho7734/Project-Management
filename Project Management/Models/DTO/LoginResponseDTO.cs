namespace Project_Management.Models.DTO
{
    public class LoginResponseDTO
    {
        public ApplicationUser User { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }
}
