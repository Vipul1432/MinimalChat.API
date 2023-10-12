using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class MessageDto
    {
       
        public string? ReceiverId { get; set; }
        public Guid? GroupId { get; set; }
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }
    }
}
