using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MinmalChat.Data.Context
{
    public class MinimalChatDbContext : IdentityDbContext<MinimalChatUser>
    {
        public MinimalChatDbContext(DbContextOptions<MinimalChatDbContext> options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the primary key for IdentityUserLogin
            modelBuilder.Entity<IdentityUserLogin<string>>().HasKey(l => new { l.LoginProvider, l.ProviderKey });

            // Configure the primary key for IdentityUserRole
            modelBuilder.Entity<IdentityUserRole<string>>().HasKey(ur => new { ur.UserId, ur.RoleId });

            // Define the relationship between the Message entity and the Sender
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

            // Configures the properties of the 'Message' entity, setting their 'IsRequired' constraints.
            // This code configures the 'Message' entity's properties, specifying whether they are required or not. 
            // It ensures that the 'SenderId' and 'Timestamp' properties are required, while 'ReceiverId', 'Content', 'GroupId', and 'FilePath' are optional.
            modelBuilder.Entity<Message>()
                   .Property(m => m.SenderId).IsRequired(true);
            modelBuilder.Entity<Message>()
                   .Property(m => m.ReceiverId).IsRequired(false);
            modelBuilder.Entity<Message>()
                  .Property(m => m.Content).IsRequired(false);
            modelBuilder.Entity<Message>()
                  .Property(m => m.Timestamp).IsRequired(true);
            modelBuilder.Entity<Message>()
                  .Property(m => m.GroupId).IsRequired(false);
            modelBuilder.Entity<Message>()
                  .Property(m => m.FilePath).IsRequired(false);

            // Configure the primary key for the GroupMember entity. 
            modelBuilder.Entity<GroupMember>()
            .HasKey(gm => new { gm.GroupId, gm.UserId });

            // Define the relationship between GroupMember and Group.
            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)     
                .HasPrincipalKey(group => group.Id);

            // Define the relationship between GroupMember and User.
            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId);
        }
    }
}
