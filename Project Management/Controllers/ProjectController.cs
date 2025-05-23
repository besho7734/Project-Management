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
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        public ProjectController(ApplicationDbContext db, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _mapper = mapper;
            _userManager = userManager;
        }
        [HttpGet("GetAllProjects")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProjectTasksViewModel>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllProjects()
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "admin")
            {
                var projects = await _db.Projects.Include(x => x.Manager)
                    .Where(x => x.ManagerId == User
                    .FindFirstValue(ClaimTypes.Name))
                    .ToListAsync();
                List<ProjectTasksViewModel> projectsDTO = new List<ProjectTasksViewModel>();
                if (projects.Any())
                {
                    foreach (var project in projects)
                    {
                        var tasks = await _db
                        .tasks
                        .Where(x => x.ProjectId == project.Id)
                        .Include(x => x.AssignedTo)
                        .ToListAsync();
                        List<TaskDTO> tasksDTO = new List<TaskDTO>();
                        foreach (var task in tasks)
                        {
                            var taskDTO = _mapper.Map<TaskDTO>(task);
                            taskDTO.ProjectName = task.Project.Name;
                            taskDTO.AssignedTo = task.AssignedTo.Email;
                            tasksDTO.Add(taskDTO);
                        }
                        var projectDTO = _mapper.Map<ProjectDTO>(project);
                        projectDTO.ManagerUserName = project.Manager.UserName;
                        ProjectTasksViewModel model = new ProjectTasksViewModel()
                        {
                            Project = projectDTO,
                            Tasks = tasksDTO
                        };
                        projectsDTO.Add(model);
                    }
                }
                var Tasks = await _db.tasks
                    .Where(x => x.UserId == User
                    .FindFirstValue(ClaimTypes.Name))
                    .ToListAsync();
                if (Tasks.Any())
                {
                    foreach (var task in Tasks)
                    {
                        var project = await _db.Projects
                            .Include(x => x.Manager)
                            .FirstOrDefaultAsync(x => x.Id == task.ProjectId);
                        var tasks = await _db.tasks
                            .Where(x => x.ProjectId == project.Id)
                            .Include(x => x.AssignedTo)
                            .ToListAsync();
                        List<TaskDTO> tasksDTO = new List<TaskDTO>();
                        foreach (var Task in tasks)
                        {
                            var taskDTO = _mapper.Map<TaskDTO>(Task);
                            taskDTO.ProjectName = task.Project.Name;
                            taskDTO.AssignedTo = task.AssignedTo.Email;
                            tasksDTO.Add(taskDTO);
                        }
                        var projectDTO = _mapper.Map<ProjectDTO>(project);
                        projectDTO.ManagerUserName = project.Manager.UserName;
                        ProjectTasksViewModel model = new ProjectTasksViewModel()
                        {
                            Project = projectDTO,
                            Tasks = tasksDTO
                        };
                        projectsDTO.Add(model);
                    }
                }
                return Ok(projectsDTO.GroupBy(x => x.Project.Id).Select(g => g.First()));
            }
            else
            {
                var projects = await _db.Projects.Include(x => x.Manager).ToListAsync();
                List<ProjectTasksViewModel> projectsDTO = new List<ProjectTasksViewModel>();
                foreach (var project in projects)
                {
                    var tasks = await _db
                        .tasks
                        .Where(x => x.ProjectId == project.Id)
                        .Include(x => x.AssignedTo)
                        .ToListAsync();
                    List<TaskDTO> tasksDTO = new List<TaskDTO>();
                    foreach (var task in tasks)
                    {
                        var taskDTO = _mapper.Map<TaskDTO>(task);
                        taskDTO.ProjectName = task.Project.Name;
                        taskDTO.AssignedTo = task.AssignedTo.Email;
                        tasksDTO.Add(taskDTO);
                    }
                    var projectDTO = _mapper.Map<ProjectDTO>(project);
                    projectDTO.ManagerUserName = project.Manager.UserName;
                    ProjectTasksViewModel model = new ProjectTasksViewModel()
                    {
                        Project = projectDTO,
                        Tasks = tasksDTO
                    };
                    projectsDTO.Add(model);
                }
                return Ok(projectsDTO);
            }
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
                return BadRequest(new { message = "Invalid Id" });
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
                taskDTO.AssignedTo = task.AssignedTo.Email;
                tasksDTO.Add(taskDTO);
            }
            ProjectTasksViewModel projectTasks = new ProjectTasksViewModel()
            {
                Project = projectDTO,
                Tasks = tasksDTO
            };
            return Ok(projectTasks);
        }
        [HttpPost("CreateProject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateProject([FromBody] ProjectCreateDTO model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Invalid project" });
            }
            var project = _mapper.Map<Project>(model);
            project.CreatedDate = DateTime.Now;
            if (model.Deadline <= project.CreatedDate)
            {
                return BadRequest(new { message = "Invalid Deadline" });
            }
            project.ManagerId = User.FindFirstValue(ClaimTypes.Name);
            await _db.Projects.AddAsync(project);
            await _db.SaveChangesAsync();
            return Ok();
        }
        [HttpPut("UpdateProject/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateProject(int Id, [FromBody] ProjectUpdateDTO model)
        {
            if (Id <= 0 || Id != model.Id)
            {
                return BadRequest(new { message = "Invalid Id" });
            }
            if (model == null)
            {
                return BadRequest(new { message = "Invalid project Update" });
            }
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == Id);
            if (model.Deadline <= project.CreatedDate)
            {
                return BadRequest(new { message = "Invalid Deadline" });
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
                return BadRequest(new { message = "Invalid Id" });
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
        [HttpPut("CreateProjectDocument/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateProjectDocument(int Id, IFormFile Document)
        {
            if (Id <= 0)
            {
                return BadRequest(new { message = "Invalid Id" });
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
            if (Document != null)
            {
                if (!string.IsNullOrEmpty(project.DocumntLocalPath))
                {
                    var oldFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), project.DocumntLocalPath);
                    FileInfo oldfile = new FileInfo(oldFileDirectory);
                    if (oldfile.Exists)
                    {
                        oldfile.Delete();
                    }
                }
                string fileName = project.Id + Path.GetExtension(Document.FileName);
                string filepath = @"wwwroot/ProjectDocuments/" + fileName;
                var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filepath);
                FileInfo file = new FileInfo(directoryLocation);
                if (file.Exists)
                {
                    file.Delete();
                }
                using (var filestream = new FileStream(directoryLocation, FileMode.Create))
                {
                    await Document.CopyToAsync(filestream);
                }
                var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                project.DocumentUrl = $"{baseUrl}/ProjectDocuments/{fileName}";
                project.DocumntLocalPath = filepath;
            }
            else
            {
                return BadRequest(new { message = "Invalid Document" });
            }
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("DeleteProjectDocument/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteProjectDocument(int Id)
        {
            if (Id <= 0)
            {
                return BadRequest(new { message = "Invalid Id" });
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
            if (!string.IsNullOrEmpty(project.DocumntLocalPath))
            {
                var oldFileDirectory = Path.Combine(Directory.GetCurrentDirectory(), project.DocumntLocalPath);
                FileInfo file = new FileInfo(oldFileDirectory);
                if (file.Exists)
                {
                    file.Delete();
                }
                project.DocumentUrl = null;
                project.DocumntLocalPath = null;
            }
            else
            {
                return BadRequest(new { message = "Invalid Document" });
            }
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
            return Ok();
        }
        [HttpGet("GetProjectDocument/{Id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK,Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDocument(int Id)
        {
            if (Id <= 0)
            {
                return BadRequest(new { message = "Invalid Id" });
            }
            var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == Id);
            if (project == null)
            {
                return NotFound(new { message = "Project not found" });
            }
            if (string.IsNullOrEmpty(project.DocumntLocalPath))
            {
                return NotFound(new { message = "There is no document to that project" });
            }
            return Ok(project.DocumentUrl);
        }
    }
}
