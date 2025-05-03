using Project_Management.Models;
using Project_Management.Models.DTO;

namespace Project_Management.Repository.IRepository
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO);
        Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO);
        Task<UserDTO> EmailConfrimation(string email, string token);
        Task<string> FrogetPassword(string email);
        Task<bool> resetPassword(ResetPasswordDTO resetPasswordDTO);
        Task<bool> ChangePassword(ChangePasswordRequestDTO changePasswordRequestDTO);
        Task<bool> DeleteUser();
        Task<bool> EditeUser(EditUserDTO model);
    }
}
