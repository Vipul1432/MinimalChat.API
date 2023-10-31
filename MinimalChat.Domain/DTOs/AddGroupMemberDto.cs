using MinimalChat.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class AddGroupMemberDto
    {
        public Guid memberId { get; set; }
        public HistoryOption HistoryOption { get; set; }
        public int? Days { get; set; }
    }
}
