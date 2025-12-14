using AIChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIChatApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatConversation> ChatConversations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.VerificationToken);
            entity.HasIndex(u => u.PasswordResetToken);
            entity.HasIndex(u => u.CreatedAt);
            entity.HasIndex(u => u.FullName);
            entity.HasIndex(u => u.UserName);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageContent).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SentAt).IsRequired();

            entity.Property(e => e.MessageContent).IsRequired(false).HasMaxLength(2000);

            entity.Property(e => e.AttachmentUrl).HasMaxLength(500);
            entity.Property(e => e.AttachmentType).HasMaxLength(50);

            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Receiver)
                .WithMany()
                .HasForeignKey(e => e.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.SenderId, e.ReceiverId, e.SentAt });
        });

        // ChatConversation configuration
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Make LastMessage nullable
            entity.Property(e => e.LastMessage)
                .HasMaxLength(100)
                .IsRequired(false); // NOT required

            entity.HasOne(e => e.User1)
                .WithMany()
                .HasForeignKey(e => e.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User2)
                .WithMany()
                .HasForeignKey(e => e.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.User1Id, e.User2Id }).IsUnique();
        });
    }
}