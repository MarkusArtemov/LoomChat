using De.Hsfl.LoomChat.Chat.Models;
using Microsoft.EntityFrameworkCore;

namespace De.Hsfl.LoomChat.Chat.Persistence
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Channel> Channels { get; set; } = null!;
        public DbSet<ChannelMember> ChannelMembers { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Channel 
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.ToTable("Channels");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(c => c.CreatedAt)
                      .IsRequired();
            });

            // ChannelMember
            modelBuilder.Entity<ChannelMember>(entity =>
            {
                entity.ToTable("ChannelMembers");

                entity.HasKey(cm => new { cm.ChannelId, cm.UserId });

                entity.Property(cm => cm.Role)
                      .HasConversion<string>();

                entity.HasOne(cm => cm.Channel)
                      .WithMany(c => c.ChannelMembers)
                      .HasForeignKey(cm => cm.ChannelId);
            });

            // ChatMessage
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("ChatMessages");

                entity.HasKey(m => m.Id);

                entity.Property(m => m.Content)
                      .IsRequired()
                      .HasMaxLength(1000);

                entity.Property(m => m.SentAt)
                      .IsRequired();

         
                entity.HasOne(m => m.Channel)
                      .WithMany(c => c.ChatMessages)
                      .HasForeignKey(m => m.ChannelId);
            });
        }
    }
}
