using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class GroupMessageDto
    {
        public List<Message?> Messages { get; set; }
        public List<GroupMember?> Members { get; set; }
    }
}
