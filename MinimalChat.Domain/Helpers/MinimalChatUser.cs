using Microsoft.AspNetCore.Identity;
using MinimalChat.Domain.Models;
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
        // Navigation property to represent the messages sent by this user
        public ICollection<Message> SentMessages { get; set; }

        // Navigation property to represent the messages received by this user
        public ICollection<Message> ReceivedMessages { get; set; }
    }

}
