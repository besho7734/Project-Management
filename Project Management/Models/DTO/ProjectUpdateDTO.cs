namespace Project_Management.Models.DTO
{
    public class ProjectUpdateDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Descriptions { get; set; }
        public DateTime Deadline { get; set; }
    }
}
