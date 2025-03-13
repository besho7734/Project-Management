namespace Project_Management.Models.DTO
{
    public class TaskDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsDone { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime Deadline { get; set; }
        public string ProjectName { get; set; }
        public string AssignedTo { get; set; }
    }
}
