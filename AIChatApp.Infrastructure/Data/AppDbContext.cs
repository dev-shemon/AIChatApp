using AIChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIChatApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add indices for frequently queried columns
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.VerificationToken);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PasswordResetToken);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.CreatedAt);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.FullName);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName);
    }
}