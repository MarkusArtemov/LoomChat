using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace De.Hsfl.LoomChat.File.Persistence
{
    public class FileDbContextFactory : IDesignTimeDbContextFactory<FileDbContext>
    {
        public FileDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables() 
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");
         
            var builder = new DbContextOptionsBuilder<FileDbContext>();
            builder.UseNpgsql(connectionString);


            return new FileDbContext(builder.Options);
        }
    }
}
