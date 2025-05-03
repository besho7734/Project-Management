using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Management.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Sender")]
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
        [ForeignKey("Receiver")]
        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;

    }
}
