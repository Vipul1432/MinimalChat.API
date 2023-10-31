using MinimalChat.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Models
{
    public class GroupMember
    {
        public required Guid GroupId { get; set; }
        public Group? Group { get; set; }
        public string UserId { get; set; }
        public MinimalChatUser? User { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime? ChatHistoryTime { get; set; }
    }
}
