using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class GroupDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public List<GroupMember>? Members { get; set; }

    }
}
