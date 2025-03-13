using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Management.Data;
using Project_Management.Models;
using Project_Management.Models.DTO;
using Project_Management.ViewModel;
using System.Security.Claims;

namespace Project_Management.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        public ProjectController(ApplicationDbContext db,IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _mapper = mapper;
            _userManager = userManager;
        }
        [HttpGet("GetAllProjects")]
        [ProducesResponseType(StatusCodes.Status200OK,Type =typeof(List<ProjectDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllProjects()
        {
            var projects = await _db.Projects.Include(x=>x.Manager).ToListAsync();
            List<ProjectDTO> projectsDTO = new List<ProjectDTO>();
            foreach (var project in projects)
            {
                var projectDTO = _mapper.Map<ProjectDTO>(project);
                projectDTO.ManagerUserName = project.Manager.UserName;
                projectsDTO.Add(projectDTO);
            }
            return Ok(projectsDTO);
        }
        [HttpGet("GetMyProjects")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProjectDTO>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyProjects()
        {
            var projects = await _db.Projects.Include(x => x.Manager).Where(x => x.ManagerId == User.FindFirstValue(ClaimTypes.Name)).ToListAsync();
            List<ProjectDTO> projectsDTO = new List<ProjectDTO>();
            foreach (var project in projects)
            {
                var projectDTO = _mapper.Map<ProjectDTO>(project);
                projectDTO.ManagerUserName = project.Manager.UserName;
                projectsDTO.Add(projectDTO);
            }
            return Ok(projectsDTO);
        }
        [HttpGet("GetOneProject/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProjectTasksViewModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOneProject(int Id)
        {
            if (Id <= 0)
            {
                return BadRequest(error: "Invalid Id");
            }
            var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == Id);
            if (project == null)
            {
                return NotFound(new { message = "Project not found" });
            }
            var projectDTO = _mapper.Map<ProjectDTO>(project);
            var user = await _userManager.FindByIdAsync(project.ManagerId);
            projectDTO.ManagerUserName = user.UserName;
            var tasks = await _db.tasks.Include(x => x.Project).Include(x => x.AssignedTo).Where(x => x.ProjectId == Id).ToListAsync();
            List<TaskDTO> tasksDTO = new List<TaskDTO>();
            foreach (var task in tasks)
            {
                var taskDTO = _mapper.Map<TaskDTO>(task);
                taskDTO.ProjectName = task.Project.Name;
                taskDTO.AssignedTo = task.AssignedTo.UserName;
                tasksDTO.Add(taskDTO);
            }
            ProjectTasksViewModel projectTasks = new ProjectTasksViewModel()
            {
                Project = projectDTO,
                Tasks = tasksDTO
            };
            return Ok(projectTasks);
        }
        //[Authorize(Roles ="admin,manager")]
        [HttpPost("CreateProject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateProject([FromBody]ProjectCreateDTO model)
        {
            if (model == null)
            {
                return BadRequest(error: "Invalid project");
            }
            var project = _mapper.Map<Project>(model);
            project.CreatedDate = DateTime.Now;
            if (model.Deadline <= project.CreatedDate)
            {
                return BadRequest(error: "Invalid Deadline");
            }
            project.ManagerId = User.FindFirstValue(ClaimTypes.Name);
            await _db.Projects.AddAsync(project);
            await _db.SaveChangesAsync();
            return Ok();
        }
        [Authorize(Roles = "admin,manager")]
        [HttpPut("UpdateProject/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize(Roles = "admin,manager")]
        public async Task<IActionResult> UpdateProject(int Id, [FromBody] ProjectUpdateDTO model)
        {
            if (Id <= 0 || Id!=model.Id)
            {
                return BadRequest(error: "Invalid Id");
            }
            if (model == null)
            {
                return BadRequest(error: "Invalid project Update");
            }
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Id);
            if (model.Deadline <= project.CreatedDate)
            {
                return BadRequest(error: "Invalid Deadline");
            }
            if (User.FindFirstValue(ClaimTypes.Role) != "admin")
            {
                if (User.FindFirstValue(ClaimTypes.Name) != project.ManagerId)
                {
                    return Forbid();
                }
            }
            if (project == null)
            {
                return NotFound(new { message = "Project not found" });
            }
            project.Deadline = model.Deadline;
            project.Name = model.Name;
            project.Descriptions = model.Descriptions;
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
            return Ok();
        }
        [HttpDelete("DeleteProject/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProject(int Id)
        {
            if (Id <= 0)
            {
                return BadRequest(error: "Invalid Id");
            }
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Id);
            if (User.FindFirstValue(ClaimTypes.Name) != project.ManagerId)
            {
                return Forbid();
            }
            if (project == null)
            {
                return NotFound(new { message = "Project not found" });
            }
            var Tasks = await _db.tasks.Where(x => x.ProjectId == Id).ToListAsync();
            _db.tasks.RemoveRange(Tasks);
            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
