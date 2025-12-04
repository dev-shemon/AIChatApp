using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIChatApp.Domain.Entities;
using AIChatApp.Infrastructure.Data;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace AIChatApp.Infrastructure.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 3)]
public class RepositoryBenchmarks
{
    private AppDbContext _context;

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // Seed test data
        var users = new List<User>();
        for (int i = 0; i < 100; i++)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                FullName = $"User {i}",
                UserName = $"user{i}",
                Email = $"user{i}@test.com",
                PasswordHash = "hash",
                ProfileImageUrl = "default.png",
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                VerifiedAt = DateTime.UtcNow
            });
        }

        _context.Users.AddRange(users);
        _context.SaveChanges();
    }

    [Benchmark]
    public async Task GetRandomUsers()
    {
        var excludedUserId = Guid.NewGuid();
        var result = await _context.Users
            .Where(u => u.Id != excludedUserId)
            .OrderBy(u => Guid.NewGuid())
            .Take(5)
            .AsNoTracking()
            .ToListAsync();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _context?.Dispose();
    }
}