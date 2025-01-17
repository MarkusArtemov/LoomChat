using Microsoft.EntityFrameworkCore;
using De.Hsfl.LoomChat.File.Models;

namespace De.Hsfl.LoomChat.File.Persistence
{

    public class FileDbContext : DbContext
    {
        public FileDbContext(DbContextOptions<FileDbContext> options)
            : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<DocumentVersion> DocumentVersions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Document
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("Documents");

                entity.HasKey(d => d.Id);

                entity.Property(d => d.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(d => d.OwnerUserId)
                      .IsRequired();

                entity.Property(d => d.CreatedAt)
                      .IsRequired();

                entity.Property(d => d.ChannelId).IsRequired();

                // 1:N Document has many DocumentVersions
                entity.HasMany(d => d.DocumentVersions)
                      .WithOne(v => v.Document)
                      .HasForeignKey(v => v.DocumentId);
            });

            // DocumentVersion
            modelBuilder.Entity<DocumentVersion>(entity =>
            {
                entity.ToTable("DocumentVersions");

                entity.HasKey(v => v.Id);

                entity.Property(v => v.VersionNumber)
                      .IsRequired();

                entity.Property(v => v.StoragePath)
                      .IsRequired()
                      .HasMaxLength(300);

                entity.Property(v => v.CreatedAt)
                      .IsRequired();

                entity.HasOne(v => v.Document)
                       .WithMany(d => d.DocumentVersions)
                       .HasForeignKey(v => v.DocumentId);

            });
            base.OnModelCreating(modelBuilder);
        }
    }
}
