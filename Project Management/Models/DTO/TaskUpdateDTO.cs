namespace Project_Management.Models.DTO
{
    public class TaskUpdateDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsDone { get; set; }
        public DateTime Deadline { get; set; }
    }
}
