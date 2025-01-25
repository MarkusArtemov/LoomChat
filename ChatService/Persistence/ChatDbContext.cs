using De.Hsfl.LoomChat.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace De.Hsfl.LoomChat.Chat.Persistence
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options)
            : base(options)
        {
        }

        // Vorhandene DbSets
        public DbSet<Channel> Channels { get; set; } = null!;
        public DbSet<ChannelMember> ChannelMembers { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

        // NEU: Für Polls
        public DbSet<Poll> Polls { get; set; } = null!;
        public DbSet<PollOption> PollOptions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ------------------
            // Channels
            // ------------------
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

            // ------------------
            // ChannelMember
            // ------------------
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

            // ------------------
            // ChatMessage
            // ------------------
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

            // ------------------
            // Poll
            // ------------------
            modelBuilder.Entity<Poll>(entity =>
            {
                entity.ToTable("Polls");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(p => p.CreatedAt)
                      .IsRequired();

                // Relationship: Channel 1 - * Poll
                entity.HasOne(p => p.Channel)
                      .WithMany(c => c.Polls)
                      .HasForeignKey(p => p.ChannelId);

            });

            // ------------------
            // PollOption
            // ------------------
            modelBuilder.Entity<PollOption>(entity =>
            {
                entity.ToTable("PollOptions");
                entity.HasKey(o => o.Id);

                entity.Property(o => o.OptionText)
                      .IsRequired()
                      .HasMaxLength(200);

                // Relationship: Poll 1 - * PollOption
                entity.HasOne(o => o.Poll)
                      .WithMany(p => p.Options)
                      .HasForeignKey(o => o.PollId);
            });
        }
    }
}
