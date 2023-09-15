
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Interfaces;
using MinmalChat.Data.Context;
using MinmalChat.Data.Services;

namespace MinimalChat.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Database connection string configuration
            builder.Services.AddDbContextPool<MinimalChatDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("MinimalChatEntities"));
            });

            // For Identity Users
            builder.Services.AddIdentity<MinimalChatUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
            }).AddEntityFrameworkStores<MinimalChatDbContext>().AddDefaultTokenProviders();


            // Registering scoped services for repository interfaces.
            // This allows for the use of dependency injection to provide instances of these repositories
            // to various parts of the application, ensuring data access is scoped to the current request.
            builder.Services.AddScoped<IUserService, UserService>();


            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}