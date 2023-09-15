using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Helpers
{
    public class MinimalChatUser : IdentityUser
    {
        [Required]
        public string Name { get; set; }
    }

}
