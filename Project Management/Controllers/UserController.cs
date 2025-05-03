using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Management.Data;
using Project_Management.Models;
using Project_Management.Models.DTO;
using Project_Management.Repository.IRepository;
using System.Security.Claims;

namespace Project_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IUserRepository _userRepo, ApplicationDbContext _db, IMapper _mapper) : ControllerBase
    {
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            var loginresponse = await _userRepo.Login(model);
            if (loginresponse.User == null || string.IsNullOrEmpty(loginresponse.Token))
            {
                return BadRequest(new { message = "Username or Password is incorrect" });
            }
            return Ok(loginresponse);

        }
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterationRequestDTO model)
        {
            bool ifUserNameUnique = _userRepo.IsUniqueUser(model.UserName);
            if (!ifUserNameUnique)
            {
                return BadRequest(new { message = "Username already exists" });
            }
            var user = await _userRepo.Register(model);
            if (user == null)
            {
                return BadRequest(new { message = "Error while registering" });
            }
            return Ok(user);
        }
        [HttpGet("EmailConfrimation/{Email},{Token}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EmailConfrimation(string Email, string Token)
        {
            var user = await _userRepo.EmailConfrimation(Email, Token);
            if (user == null)
            {
                return BadRequest(new { message = "Error while confirming email" });
            }
            return Ok(new { message = "Email Confirmed" });
        }
        [HttpPost("ForgetPassword/{Email}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgetPassword(string Email)
        {
            var user = await _userRepo.FrogetPassword(Email);
            if (user == null)
            {
                return BadRequest(new { message = "Error while sending email" });
            }
            return Ok(user);
        }
        [HttpPost("ResetPassword")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            var user = await _userRepo.resetPassword(model);
            if (!user)
            {
                return BadRequest(error: "Error while reseting password");
            }
            return Ok(new { message = "Password Reset Done" });
        }

        [Authorize]
        [HttpPut("ChangePassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO model)
        {
            var user = await _userRepo.ChangePassword(model);
            if (!user)
            {
                return BadRequest(error: "Error while Changing password");
            }
            return Ok(new { message = "Password Changed successfully" });
        }
        [Authorize]
        [HttpDelete("DeleteAccunt")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAccunt()
        {
            var result = await _userRepo.DeleteUser();
            if (!result)
            {
                return BadRequest(error: "Error while Deleting Account");
            }
            return Ok(new { message = "Account Deleted successfully" });
        }
        [Authorize]
        [HttpGet("ChatHome")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ChatHome()
        {
            var userid = User.FindFirstValue(ClaimTypes.Name);
            var messages = _db.chatMessages.Where(x => x.SenderId == userid || x.ReceiverId == userid).OrderByDescending(x => x.CreatedAt).ToList();
            List<HomeChatDTO> UsersDTO = new List<HomeChatDTO>();
            foreach (var message in messages)
            {
                var userDTO = new HomeChatDTO();
                if (userid == message.SenderId)
                {
                    var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Id == message.ReceiverId);
                    userDTO.Id = user.Id;
                    userDTO.UserName = user.UserName;
                    userDTO.Message = message.Message;
                    UsersDTO.Add(userDTO);
                }
                else if (userid == message.ReceiverId)
                {
                    var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.Id == message.SenderId);
                    userDTO.Id = user.Id;
                    userDTO.UserName = user.UserName;
                    userDTO.Message = message.Message;
                    UsersDTO.Add(userDTO);
                }
            }
            UsersDTO = UsersDTO.GroupBy(u => u.Id).Select(g => g.First()).ToList();
            return Ok(UsersDTO);
        }
        [Authorize]
        [HttpPost("send/{id},{message}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> send(string id, string message)
        {
            var userid = User.FindFirstValue(ClaimTypes.Name);
            ChatMessage chatMessage = new ChatMessage
            {
                SenderId = userid,
                ReceiverId = id,
                Message = message
            };
            await _db.chatMessages.AddAsync(chatMessage);
            await _db.SaveChangesAsync();
            return Ok();
        }
        [Authorize]
        [HttpPut("Edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Edit([FromBody] EditUserDTO model)
        {
            var result = await _userRepo.EditeUser(model);
            if (!result)
            {
                return BadRequest(error: "Error while Editing Account");
            }
            return Ok();
        }

    }
}
