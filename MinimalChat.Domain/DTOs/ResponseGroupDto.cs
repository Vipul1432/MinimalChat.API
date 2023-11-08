using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class ResponseGroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
