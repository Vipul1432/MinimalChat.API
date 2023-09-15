using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.Helpers;
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
    }
}
