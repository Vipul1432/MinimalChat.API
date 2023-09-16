using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Models
{
    public class RequestLog
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public DateTime RequestTimestamp { get; set; }
        public string Username { get; set; }
        public string RequestBody { get; set; }
    }

}
