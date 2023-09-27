using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class ConversationDto
    {
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}
