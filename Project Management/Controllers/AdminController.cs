using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Management.Data;
using Project_Management.Models;
using Project_Management.Models.DTO;
using SendGrid.Helpers.Mail;
using System.Security.Claims;

namespace Project_Management.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        public AdminController(ApplicationDbContext db,UserManager<ApplicationUser> userManager,IMapper mapper)
        {
            _db = db;
            _userManager = userManager;
            _mapper = mapper;
        }
        [HttpPut("ChangeRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangeRole([FromBody]ChangeRoleRequest model)
        {
            var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.UserName == model.UserName);
            if (user == null) return NotFound(new {Messege = "Invalid User Name"});
            var roles = await _userManager.GetRolesAsync(user);
            var result1 = await _userManager.RemoveFromRolesAsync(user, roles.ToArray());
            if (result1.Succeeded)
            {
                var result = await _userManager.AddToRoleAsync(user, model.NewRole);
                if (!result.Succeeded)
                {
                    return BadRequest(new { Messege = "Role Not Found" });
                }
                return Ok();
            }

            return BadRequest(new { Messege = "Role Not Found" }); ;
        }
        [HttpGet("GetAllUsers")]
        [ProducesResponseType(StatusCodes.Status200OK,Type =typeof(List<GetUserToReturnDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> getAllUsers()
        {
            var users = await _db.applicationUsers.ToListAsync();
            List<GetUserToReturnDTO> UsersDTO = new List<GetUserToReturnDTO>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userDTO = _mapper.Map<GetUserToReturnDTO>(user); 
                userDTO.Role = roles.FirstOrDefault();
                UsersDTO.Add(userDTO);
            }
            return Ok(UsersDTO);
        }
        [HttpGet("GetUser/{UserName}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetUserToReturnDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUser(string UserName)
        {
            var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.UserName == UserName);
            if (user == null) return NotFound(new { Messege = "Invalid User Name" });
            var roles = await _userManager.GetRolesAsync(user);
            var userDTO = _mapper.Map<GetUserToReturnDTO>(user);
            userDTO.Role = roles.FirstOrDefault();
            return Ok(userDTO);
        }
        [HttpDelete("DeleteUser/{UserName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUser(string UserName)
        {
            var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.UserName == UserName);
            if (user == null) return NotFound(new { Messege = "Invalid User Name" });
            var projects = await _db.Projects.Where(p => p.ManagerId == user.Id).ToListAsync();
            foreach (var project in projects)
            {
                project.ManagerId = User.FindFirstValue(ClaimTypes.Name);
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
            if (result.Succeeded)
            {
                return Ok();
            }
            return BadRequest(new { message = "Error while deleting the user" });
        }
    }
}
