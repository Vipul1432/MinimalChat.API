using MinimalChat.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

        public string? ReceiverId { get; set; }
        [Required]
        [MaxLength(1000)] 
        public string? Content { get; set; }
        [Required]
        public DateTime Timestamp { get; set; }

        // Group Id for group messages
        public Guid? GroupId { get; set; }

        // File path or file name property for uploaded files
        public string? FilePath { get; set; }

        // Navigation property to represent the sender of this message
        public MinimalChatUser Sender { get; set; }

        // Navigation property to represent the receiver of this message
        public MinimalChatUser Receiver { get; set; }

        // Navigation property to represent the group of this message (for group messages)
        public Group Group { get; set; }
    }

}
