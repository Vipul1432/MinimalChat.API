using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Models
{
    public class Group
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }

        // Navigation property for GroupMembers
        public List<GroupMember>? Members { get; set; }

        // Navigation property for Messages
        public List<Message>? Messages { get; set; }
    }
}
