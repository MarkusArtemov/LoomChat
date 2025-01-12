using De.Hsfl.LoomChat.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace De.Hsfl.LoomChat.Auth.Persistence
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;


        // Sets up constraints for the User table
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(u => u.Id);

                entity.Property(u => u.Username)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(u => u.Username)
                    .IsUnique();

                entity.Property(u => u.PasswordHash)
                    .IsRequired();

                entity.Property(u => u.CreatedAt)
                    .IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }

    }
}
