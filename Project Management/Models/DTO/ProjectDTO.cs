namespace Project_Management.Models.DTO
{
    public class ProjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Descriptions { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime Deadline { get; set; }
        public string ManagerUserName { get; set; }
        public string? DocumentUrl { get; set; }
    }
}
