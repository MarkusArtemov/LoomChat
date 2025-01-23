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

                // new: file type
                entity.Property(d => d.FileType)
                      .IsRequired()
                      .HasMaxLength(200);

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

                // new: is it a full copy or a delta?
                entity.Property(v => v.IsFull)
                      .IsRequired();

                // new: which version does the delta base on?
                entity.Property(v => v.BaseVersionId)
                      .IsRequired(false);

                entity.HasOne(v => v.Document)
                       .WithMany(d => d.DocumentVersions)
                       .HasForeignKey(v => v.DocumentId);
            });
        }

    }
}
