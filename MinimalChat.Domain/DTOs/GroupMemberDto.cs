using MinimalChat.Domain.Enum;
using MinimalChat.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class GroupMemberDto
    {
        public Guid GroupId { get; set; }
        public string UserId { get; set; }
        public bool IsAdmin { get; set; }
        public string UserName { get; set; }
    }
}
