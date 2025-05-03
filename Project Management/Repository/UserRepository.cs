using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project_Management.Data;
using Project_Management.Models;
using Project_Management.Models.DTO;
using Project_Management.Repository.IRepository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Project_Management.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailsender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretkey;
        public UserRepository(ApplicationDbContext db, IConfiguration configuration
            , UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager , IEmailService emailSender, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _mapper = mapper;
            _emailsender = emailSender;
            _userManager = userManager;
            _roleManager = roleManager;
            secretkey = configuration.GetValue<string>("ApiSettings:Secret");
            _httpContextAccessor = httpContextAccessor;
        }
        public bool IsUniqueUser(string username)
        {
            var User = _db.applicationUsers.FirstOrDefault(x => x.UserName == username);
            if (User == null) return true;
            return false;
        }
        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Email == loginRequestDTO.Email);
            bool isvalid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);
            if (user == null || isvalid == false)
            {
                return new LoginResponseDTO()
                {
                    Token = "",
                    User = null
                };
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretkey);
            var roles = await _userManager.GetRolesAsync(user);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name , user.Id.ToString()),
                    new Claim(ClaimTypes.Role , roles.FirstOrDefault())
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            LoginResponseDTO loginResponseDTO = new LoginResponseDTO()
            {
                Token = tokenHandler.WriteToken(token),
                User =  user,
                Role = roles.FirstOrDefault()
            };
            return loginResponseDTO;
        }
        public async Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO)
        {
            ApplicationUser user = new()
            {
                UserName = registerationRequestDTO.UserName,
                FullName = registerationRequestDTO.FullName,
                Email = registerationRequestDTO.Email,
                PhoneNumber = registerationRequestDTO.PhoneNumber,
                NormalizedEmail = registerationRequestDTO.Email.ToUpper(),
                bio = registerationRequestDTO.Bio
            };
            try
            {
                var Users = await _db.applicationUsers.ToListAsync();
                if (!Users.Any())
                {
                    var result = await _userManager.CreateAsync(user, registerationRequestDTO.Password);
                    if (result.Succeeded)
                    {
                        if (!_roleManager.RoleExistsAsync("admin").GetAwaiter().GetResult())
                        {
                            await _roleManager.CreateAsync(new IdentityRole("admin"));
                            await _roleManager.CreateAsync(new IdentityRole("manager"));
                            await _roleManager.CreateAsync(new IdentityRole("user"));
                        }
                        await _userManager.AddToRoleAsync(user, "admin");
                        var UserToReturn = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Email == registerationRequestDTO.Email);
                        var EmailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = $"https://localhost:7191/api/User/EmailConfrimation/{user.Email},{EmailToken}";
                        var userDTO = _mapper.Map<UserDTO>(UserToReturn);
                        userDTO.EmailConfirmationToken = EmailToken;
                        await _emailsender.SendEmailAsync(user.Email, "Confirm your email", $"<p>Your Email ConfirmationLink {confirmationLink}</p>");
                        return userDTO;
                    }
                }
                else
                {
                    var result = await _userManager.CreateAsync(user, registerationRequestDTO.Password);
                    if (result.Succeeded)
                    {
                        if (!_roleManager.RoleExistsAsync("admin").GetAwaiter().GetResult())
                        {
                            await _roleManager.CreateAsync(new IdentityRole("admin"));
                            await _roleManager.CreateAsync(new IdentityRole("manager"));
                            await _roleManager.CreateAsync(new IdentityRole("user"));
                        }
                        await _userManager.AddToRoleAsync(user, "user");
                        var UserToReturn = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Email == registerationRequestDTO.Email);
                        var EmailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = $"https://localhost:7191/api/User/EmailConfrimation/{user.Email},{EmailToken}";
                        var userDTO = _mapper.Map<UserDTO>(UserToReturn);
                        userDTO.EmailConfirmationToken = EmailToken;
                        await _emailsender.SendEmailAsync(user.Email, "Confirm your email", $"<p>Your Email ConfirmationLink {confirmationLink}</p>");
                        return userDTO;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return new UserDTO();
        }
        public async Task<UserDTO> EmailConfrimation(string Email, string Token)
        {
            var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Email == Email);
            if (user == null) return null;
            var result = await _userManager.ConfirmEmailAsync(user, Token);
            if (result.Succeeded)
            {
                return _mapper.Map<UserDTO>(user);
            }
            return null;
        }
        public async Task<string> FrogetPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _emailsender.SendEmailAsync(user.Email, "Reset Password", $"<p>Your Reset Password code {token}</p>");
            return token;
        }
        public async Task<bool> resetPassword(ResetPasswordDTO resetPasswordDTO)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDTO.Email);
            if (user == null) return false;
            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDTO.Token, resetPasswordDTO.NewPassword);
            if (result.Succeeded) return true;
            return false;
        }
        public async Task<bool> ChangePassword(ChangePasswordRequestDTO changePasswordRequestDTO)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            var user = await _userManager.FindByIdAsync(userId);
            var result = await _userManager.ChangePasswordAsync(user,changePasswordRequestDTO.OldPassword,changePasswordRequestDTO.NewPassword);
            if (result.Succeeded) return true;
            return false;
        }
        public async Task<bool> DeleteUser()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.FirstOrDefault() == "manager") {
                var projects = await _db.Projects.Where(p => p.ManagerId == user.Id).ToListAsync();
                var admins = await _userManager.GetUsersInRoleAsync("admin");
                foreach (var project in projects)
                {
                    project.ManagerId = admins.FirstOrDefault().Id;
                }
            }
            var messages = await _db.chatMessages.Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id).ToListAsync();
            _db.chatMessages.RemoveRange(messages);
            var tasks = await _db.tasks.Where(t => t.UserId == user.Id).ToListAsync();
            foreach (var task in tasks)
            {
                var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == task.ProjectId);
                task.UserId = project.ManagerId;
            }
            await _db.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded) return true;
            return false;
        }
        public async Task<bool> EditeUser(EditUserDTO model)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            user.FullName = model.FullName;
            user.bio = model.Bio;
            if(user.Email != model.Email)
            {
                user.EmailConfirmed = false;
                string token = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);
                var confirmationLink = $"https://localhost:7191/api/User/EmailConfrimation/{model.Email},{token}";
                await _emailsender.SendEmailAsync(model.Email, "Confirm your email", $"<p>Your Email ConfirmationLink {confirmationLink}</p>");
            }
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return true;
            return false;
        }
    }
}
