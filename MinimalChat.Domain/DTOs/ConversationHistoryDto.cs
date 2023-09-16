using MinimalChat.Domain.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class ConversationHistoryDto
    {
        [Required]
        public Guid UserId { get; set; }

        public DateTime Before { get; set; } = DateTime.Now;

        [Range(1, int.MaxValue)]
        public int Count { get; set; } = 20;

        public SortOrder SortOrder { get; set; } = SortOrder.asc;
    }
}
