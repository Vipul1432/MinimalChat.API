
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalChat.API.Hubs;
using MinimalChat.API.Middleware;
using MinimalChat.Domain.Helpers;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using MinmalChat.Data.Repository;
using MinmalChat.Data.Services;
using System.Text;

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
                options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            });

            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("JWT"));

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

            // Configures authentication services with JWT Bearer authentication.
            ConfigurationManager Configuration = builder.Configuration;
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })

            //Adds JWT Bearer authentication options to the authentication services.
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = Configuration["JWT:ValidAudience"],
                    ValidIssuer = Configuration["JWT:ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Secret"])),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            //builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

            // Registering scoped services for repository interfaces.
            // This allows for the use of dependency injection to provide instances of these repositories
            // to various parts of the application, ensuring data access is scoped to the current request.
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddScoped<ILogService, LogService>();
            builder.Services.AddScoped<IGroupService, GroupService>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Add SignalR Service
            builder.Services.AddSignalR();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Configure CORS (Cross-Origin Resource Sharing) policy
            // Allow requests from url origin ( "http://localhost:4200")
            // Allow any HTTP method (GET, POST, PUT, DELETE, etc.)
            // Allow any HTTP headers in the request            
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowOrigin",
                    builder =>
                    {
                        builder
                            .WithOrigins("http://localhost:4200") // Allow requests from your Angular app's URL
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials(); // Allow credentials (cookies, Authorization header)
                    });
            });

            // Define and configure Swagger documentation settings for API.
            builder.Services.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "IdentityApi", Version = "v1" });
                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please Enter a valid Token!",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
          
            app.UseHttpsRedirection();
            app.UseCors("AllowOrigin");
            app.UseAuthentication();
            app.UseAuthorization();

            // Request Logging Middleware
            app.UseRequestLoggingMiddleware();

            app.MapControllers();

            // chatHub for realtime chat
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}