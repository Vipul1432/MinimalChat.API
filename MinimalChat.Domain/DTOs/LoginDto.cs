using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
