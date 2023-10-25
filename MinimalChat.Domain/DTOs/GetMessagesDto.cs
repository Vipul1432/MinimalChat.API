using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class GetMessagesDto
    {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }


        public string? ReceiverId { get; set; }
        public Guid? GroupId { get; set; }
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        public string? FilePath { get; set; }
        public string? FileName { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public List<GroupMemberDto?> Users { get; set; }
    }
}
