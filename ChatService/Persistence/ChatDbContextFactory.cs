using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace De.Hsfl.LoomChat.Chat.Persistence
{
    public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
    {
        public ChatDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("No connection string 'DefaultConnection' found.");

            var builder = new DbContextOptionsBuilder<ChatDbContext>();
            builder.UseNpgsql(connectionString);

            return new ChatDbContext(builder.Options);
        }
    }
}
