using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Management.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Descriptions { get; set; }
        public DateTime CreatedDate  { get; set; }
        public DateTime Deadline  { get; set; }

        [ForeignKey("Manager")]
        public string ManagerId { get; set; }
        public ApplicationUser Manager { get; set; }

    }
}
