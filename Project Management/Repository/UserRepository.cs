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
        public bool IsUniqueEmail(string email)
        {
            var User = _db.applicationUsers.FirstOrDefault(x => x.Email == email);
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
            if (registerationRequestDTO.Image != null)
            {
                string fileName = user.Id + Path.GetExtension(registerationRequestDTO.Image.FileName);
                string filepath = @"wwwroot/ProfilePic/" + fileName;
                var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filepath);
                FileInfo file = new FileInfo(directoryLocation);
                if (file.Exists)
                {
                    file.Delete();
                }
                using (var filestream = new FileStream(directoryLocation, FileMode.Create))
                {
                    await registerationRequestDTO.Image.CopyToAsync(filestream);
                }
                var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}{_httpContextAccessor.HttpContext.Request.PathBase.Value}";
                user.ImageUrl = $"{baseUrl}/ProfilePic/{fileName}";
                user.ImageLocalPath = filepath;

            }
            else
            {
                user.ImageUrl = "https://placehold.co/600*400";
            }
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
                        var confirmationLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}{_httpContextAccessor.HttpContext.Request.PathBase.Value}/{user.Email},{EmailToken}";
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
                        var confirmationLink = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}{_httpContextAccessor.HttpContext.Request.PathBase.Value}/api/User/EmailConfrimation/{user.Email},{EmailToken}";
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
            return null;
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
            var projects = await _db.Projects.Where(p => p.ManagerId == user.Id).ToListAsync();
            if (projects.Any())
            {
                foreach (var project in projects)
                {
                    var tasksremoved = await _db.tasks.Where(x => x.ProjectId == project.Id).ToListAsync();
                    _db.tasks.RemoveRange(tasksremoved);
                }
                await _db.SaveChangesAsync();
                _db.Projects.RemoveRange(projects);
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
            if (!string.IsNullOrEmpty(user.ImageLocalPath))
            {
                var oldFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), user.ImageLocalPath);
                FileInfo file = new FileInfo(oldFileDirectory);
                if (file.Exists)
                {
                    file.Delete();
                }
            }
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded) return true;
            return false;
        }
        public async Task<bool> EditeUser(EditUserDTO model)
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            var UserName = await _db.applicationUsers
                .Where(x => x.Id != userId)
                .FirstOrDefaultAsync(x => x.UserName == model.UserName);
            if (UserName != null) return false; 
            var Email = await _db.applicationUsers
                .Where(x => x.Id != userId)
                .FirstOrDefaultAsync(x => x.Email == model.Email);
            if (Email != null) return false;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;
            user.UserName = model.UserName;
            user.bio = model.Bio;
            user.PhoneNumber = model.PhoneNumber;
            if (user.Email != model.Email)
            {
                user.Email = model.Email;
                user.EmailConfirmed = false;
                string token = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);
                var confirmationLink = $"https://frankly-refined-escargot.ngrok-free.app/{model.Email},{token}";
                await _emailsender.SendEmailAsync(model.Email, "Confirm your email", $"<p>Your Email ConfirmationLink {confirmationLink}</p>");
            }
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return true;
            return false;
        }
        public async Task<GetUserToReturnDTO> GetUser()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null) return null;
            var roles = await _userManager.GetRolesAsync(user);
            var userDTO = _mapper.Map<GetUserToReturnDTO>(user);
            userDTO.Role = roles.FirstOrDefault();
            return userDTO;
        }

        public async Task<List<GetUserToReturnDTO>> GetUsers()
        {
            var users = await _db.applicationUsers
                .Where(x => x.Id != _httpContextAccessor.HttpContext.User
                .FindFirstValue(ClaimTypes.Name)).ToListAsync();
            List<GetUserToReturnDTO> UsersDTO = new List<GetUserToReturnDTO>();
            foreach(var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userDTO = _mapper.Map<GetUserToReturnDTO>(user);
                userDTO.Role = roles.FirstOrDefault();
                UsersDTO.Add(userDTO);
            }
            return UsersDTO;
        }

        public async Task<bool> ChangeProfilPic(IFormFile NewImage)
        {
            var user = await _userManager
                .FindByIdAsync(_httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name));

            if (NewImage != null)
            {
                if (!string.IsNullOrEmpty(user.ImageLocalPath))
                {
                    var oldFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), user.ImageLocalPath);
                    FileInfo file = new FileInfo(oldFileDirectory);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                }

                string fileName = user.Id + Path.GetExtension(NewImage.FileName);
                string filepath = @"wwwroot/ProfilePic/" + fileName;
                var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filepath);
                using (var filestream = new FileStream(directoryLocation, FileMode.Create))
                {
                    await NewImage.CopyToAsync(filestream);
                }
                var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}{_httpContextAccessor.HttpContext.Request.PathBase.Value}";
                user.ImageUrl = $"{baseUrl}/ProfilePic/{fileName}";
                user.ImageLocalPath = filepath;

            }
            else
            {
                user.ImageUrl = "https://placehold.co/600*400";
            }
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return true;
            return false;
        }

        public async Task<bool> DeleteProfilePic()
        {
            var user = await _userManager
               .FindByIdAsync(_httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name));
            if (!string.IsNullOrEmpty(user.ImageLocalPath))
            {
                var oldFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), user.ImageLocalPath);
                FileInfo file = new FileInfo(oldFileDirectory);
                if (file.Exists)
                {
                    file.Delete();
                }
            }
            user.ImageUrl = "https://placehold.co/600*400";
            user.ImageLocalPath = null;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return true;
            return false;
        }

    }
}
