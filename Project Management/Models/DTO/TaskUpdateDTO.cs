namespace Project_Management.Models.DTO
{
    public class TaskUpdateDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AssignedTo { get; set; }
        public DateTime Deadline { get; set; }
    }
}
