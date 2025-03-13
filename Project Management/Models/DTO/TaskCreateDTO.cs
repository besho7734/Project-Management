namespace Project_Management.Models.DTO
{
    public class TaskCreateDTO
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public int ProjectId { get; set; }
        public string AssignedTo { get; set; }
    }
}
