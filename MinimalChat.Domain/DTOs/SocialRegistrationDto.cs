using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class SocialRegistrationDto
    {
        public string Email { get; set; }
        public string Name { get; set; }
    }

}
