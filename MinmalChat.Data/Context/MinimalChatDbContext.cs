using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinmalChat.Data.Context
{
    public class MinimalChatDbContext : IdentityDbContext<MinimalChatUser>
    {
        public MinimalChatDbContext(DbContextOptions<MinimalChatDbContext> options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }
    }
}
