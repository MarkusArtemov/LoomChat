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
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!; // TPH für TextMessage + PollMessage
        public DbSet<Poll> Polls { get; set; } = null!;
        public DbSet<PollOption> PollOptions { get; set; } = null!;

        // NEU: DbSet für die Votes
        public DbSet<PollVote> PollVotes { get; set; } = null!;

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

                entity.Property(cm => cm.Role).HasConversion<string>();

                entity.HasOne(cm => cm.Channel)
                      .WithMany(c => c.ChannelMembers)
                      .HasForeignKey(cm => cm.ChannelId);
            });

            // ------------------
            // ChatMessage (TPH)
            // ------------------
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("ChatMessages");
                entity.HasKey(m => m.Id);

                // TPH-Discriminator
                entity
                    .HasDiscriminator<string>("MessageType")
                    .HasValue<ChatMessage>("text")   // Basisklasse
                    .HasValue<TextMessage>("text")   // Genauere Implementierung
                    .HasValue<PollMessage>("poll");

                // Felder in der Basisklasse
                entity.Property(m => m.SentAt)
                      .IsRequired();

                entity.HasOne(m => m.Channel)
                      .WithMany(c => c.ChatMessages)
                      .HasForeignKey(m => m.ChannelId);
            });

            // Einschränkungen für TextMessage
            modelBuilder.Entity<TextMessage>(entity =>
            {
                entity.Property(tm => tm.Content)
                      .IsRequired()
                      .HasMaxLength(1000);
            });

            // PollMessage => 1:1 oder 1:N
            modelBuilder.Entity<PollMessage>(entity =>
            {
                entity.HasOne(pm => pm.Poll)
                      .WithMany() // Falls Poll eine ICollection<PollMessage> hat, kann man .WithMany(p => p.PollMessages) nutzen
                      .HasForeignKey(pm => pm.PollId);
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

                entity.HasOne(o => o.Poll)
                      .WithMany(p => p.Options)
                      .HasForeignKey(o => o.PollId);
            });

            // ------------------
            // NEU: PollVote
            // ------------------
            modelBuilder.Entity<PollVote>(entity =>
            {
                entity.ToTable("PollVotes");
                entity.HasKey(v => v.Id);

                entity.Property(v => v.UserId).IsRequired();

                // Beziehung zu Poll
                entity.HasOne(v => v.Poll)
                      .WithMany() // oder .WithMany(p => p.PollVotes) wenn du eine Collection in Poll anlegst
                      .HasForeignKey(v => v.PollId);

                // Beziehung zu PollOption
                entity.HasOne(v => v.PollOption)
                      .WithMany() // oder .WithMany(o => o.Votes)
                      .HasForeignKey(v => v.PollOptionId);
            });
        }
    }
}
