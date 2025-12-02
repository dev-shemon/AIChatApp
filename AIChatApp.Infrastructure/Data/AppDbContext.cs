using AIChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIChatApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
}