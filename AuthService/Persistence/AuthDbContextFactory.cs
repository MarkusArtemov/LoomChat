using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace De.Hsfl.LoomChat.Auth.Persistence
{

    /// <summary>
    /// Creates AuthDbContext instances at design time for EF Core migrations and tooling.
    /// This class allows EF to generate or update the database schema without running the full application.
    /// </summary>
    public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
    {
        public AuthDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No connection string 'DefaultConnection' found.");

            var builder = new DbContextOptionsBuilder<AuthDbContext>();
            builder.UseNpgsql(connectionString);

            return new AuthDbContext(builder.Options);
        }
    }
}
