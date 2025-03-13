using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Management.Data;
using Project_Management.Models;
using Project_Management.Models.DTO;
using System.Security.Claims;

namespace Project_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        public TaskController(ApplicationDbContext db, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _mapper = mapper;
            _userManager = userManager;
        }
        [HttpGet("GetAllTasks")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TaskDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllTasks()
        {
            var tasks = await _db.tasks.Include(x => x.Project).Include(x => x.AssignedTo).ToListAsync();
            List<TaskDTO> tasksDTO = new List<TaskDTO>();
            foreach (var task in tasks)
            {
                var taskDTO = _mapper.Map<TaskDTO>(task);
                taskDTO.ProjectName = task.Project.Name;
                taskDTO.AssignedTo = task.AssignedTo.UserName;
                tasksDTO.Add(taskDTO);
            }
            return Ok(tasksDTO);
        }
        [HttpGet("GetOneTask/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaskDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOneTask(int Id)
        {
            if (Id <= 0)
            {
                return BadRequest(error: "Invalid Id");
            }

            var task = await _db.tasks.Include(x => x.Project).Include(x => x.AssignedTo).FirstOrDefaultAsync(x => x.Id == Id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }
            TaskDTO taskDTO = _mapper.Map<TaskDTO>(task);
            taskDTO.ProjectName = task.Project.Name;
            taskDTO.AssignedTo = task.AssignedTo.UserName;
            return Ok(taskDTO);
        }
        [HttpGet("GetMyTasks")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TaskDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyTasks()
        {
            var tasks = await _db.tasks.Include(x => x.Project).Include(x => x.AssignedTo).Where(x => x.UserId == User.FindFirstValue(ClaimTypes.Name)).ToListAsync();
            List<TaskDTO> tasksDTO = new List<TaskDTO>();
            foreach (var task in tasks)
            {
                var taskDTO = _mapper.Map<TaskDTO>(task);
                taskDTO.ProjectName = task.Project.Name;
                taskDTO.AssignedTo = task.AssignedTo.UserName;
                tasksDTO.Add(taskDTO);
            }
            return Ok(tasksDTO);
        }
        [HttpPost("CreateTask")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDTO model)
        {
            if (model == null)
            {
                return BadRequest(error: "Invalid Tasks");
            }
            if (model.Deadline <= DateTime.Now)
            {
                return BadRequest(error: "Invalid Deadline");
            }
            var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == model.ProjectId);
            if (project == null)
            {
                return BadRequest(error: "Invalid ProjectId");
            }
            var user = await _db.applicationUsers.FirstOrDefaultAsync(x => x.UserName == model.AssignedTo);
            if (user == null)
            {
                return BadRequest(error: "Invalid Assigned UserName");
            }
            task task = new task();
            task.Title = model.Title;
            task.Description = model.Description;
            task.Deadline = model.Deadline;
            task.ProjectId = model.ProjectId;
            task.IsDone = false;
            task.CreatedDate = DateTime.Now;
            task.UserId = user.Id;
            await _db.tasks.AddAsync(task);
            await _db.SaveChangesAsync();
            return Ok();
        }
        [HttpDelete("DeleteTask/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteTask(int Id)
        {
            if (Id <= 0)
            {
                return BadRequest(error: "Invalid Id");
            }

            var task = await _db.tasks.FirstOrDefaultAsync(x => x.Id == Id);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }
            _db.tasks.Remove(task);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("UpdateTask/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateTask(int Id, [FromBody] TaskUpdateDTO model )
        {
            if (Id <= 0 || Id !=model.Id)
            {
                return BadRequest(error: "Invalid Id");
            }
            if(model == null)
            {
                return BadRequest(error: "Invalid Task Update");
            }
            var task = await _db.tasks.AsNoTracking().Include(x=>x.Project).FirstOrDefaultAsync(x => x.Id == Id);
            if (model.Deadline <= task.CreatedDate)
            {
                return BadRequest(error: "Invalid Deadline");
            }
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }
            if (User.FindFirstValue(ClaimTypes.Role) != "admin")
            {
                if (User.FindFirstValue(ClaimTypes.Name) != task.Project.ManagerId)
                {
                    if (User.FindFirstValue(ClaimTypes.Name) != task.UserId)
                    {
                        return Forbid();
                    }
                }
            }
            task.Deadline = model.Deadline;
            task.Title = model.Title;
            task.Description = model.Description;
            task.IsDone = model.IsDone;
            _db.tasks.Update(task);
            await _db.SaveChangesAsync();
            return Ok();
        }

    }
}
