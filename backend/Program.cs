
using backend.Data;
using backend.Hubs;
using backend.Interfaces;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add SignalR
            builder.Services.AddSignalR(options =>
            {
                // Limit message size to 1MB
                options.MaximumReceiveMessageSize = 1024 * 1024;
                options.EnableDetailedErrors = true;
            });

            var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                  policy =>
                                  {
                                      policy.WithOrigins("http://localhost:3000")
                                            .AllowAnyMethod()
                                            .AllowAnyHeader()
                                            .AllowCredentials();
                                  }
                                  );
            });

            // Add DbContext
            builder.Services.AddDbContext<BackendAppDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            // Add services
            builder.Services.AddTransient<IPlayer, PlayerService>();
            builder.Services.AddTransient<IGame, GameService>();
            builder.Services.AddTransient<Interfaces.ISession, SessionService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCors(MyAllowSpecificOrigins);

            app.MapHub<GameSessionHub>("/sessionHub").RequireCors(MyAllowSpecificOrigins);

            // Apply migrations automatically at startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BackendAppDbContext>();
                dbContext.Database.Migrate(); // Apply migrations if needed
            }
            app.MapControllers();

            app.Run();
        }
    }
}
