using Project_Management.Models.DTO;

namespace Project_Management.ViewModel
{
    public class ProjectTasksViewModel
    {
        public ProjectDTO Project { get; set; }
        public List<TaskDTO> Tasks { get; set; }
    }
}
