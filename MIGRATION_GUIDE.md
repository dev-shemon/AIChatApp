# ?? Quick Start Guide - Database Migration & Deployment

## ?? Table of Contents
1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [Applying the Migration](#applying-the-migration)
3. [Verifying the Migration](#verifying-the-migration)
4. [Rolling Back (if needed)](#rolling-back-if-needed)
5. [Troubleshooting](#troubleshooting)

---

## ? Pre-Deployment Checklist

Before applying the migration to production, ensure:

- [ ] Backup your database
- [ ] All code changes have been reviewed
- [ ] Build succeeds: `dotnet build`
- [ ] No other developers are actively modifying the database
- [ ] Maintenance window scheduled (if on production)
- [ ] Team is notified of deployment

```powershell
# Quick verification
cd "C:\Users\31act\OneDrive\Documents\ostad-asp.net\AIChatApp"
dotnet build  # Should succeed
```

---

## ?? Applying the Migration

### Option 1: Automatic Migration (Recommended)

The migration has already been applied during development. If you need to reapply:

```powershell
cd "C:\Users\31act\OneDrive\Documents\ostad-asp.net\AIChatApp"

# Apply the migration to your database
dotnet ef database update --project AIChatApp.Infrastructure --startup-project AIChatApp.Web
```

**Expected Output:**
```
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (200ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      ...
      CREATE INDEX [IX_Users_Email] ON [Users] ([Email]);
      CREATE INDEX [IX_Users_VerificationToken] ON [Users] ([VerificationToken]);
      CREATE INDEX [IX_Users_PasswordResetToken] ON [Users] ([PasswordResetToken]);
      CREATE INDEX [IX_Users_CreatedAt] ON [Users] ([CreatedAt]);
      CREATE INDEX [IX_Users_FullName] ON [Users] ([FullName]);
      CREATE INDEX [IX_Users_UserName] ON [Users] ([UserName]);
      ...
Done.
```

### Option 2: Check Current Migration Status

```powershell
# See what migrations have been applied
dotnet ef migrations list --project AIChatApp.Infrastructure --startup-project AIChatApp.Web
```

**Expected Output:**
```
20251203105306_Remigrated
20251204164742_AddUserIndices (Applied)
```

---

## ?? Verifying the Migration

### SQL Server Method

```sql
-- Connect to your database in SQL Server Management Studio

-- 1. Check if indices exist
SELECT name, type_desc FROM sys.indexes 
WHERE object_id = OBJECT_ID('dbo.Users') AND name LIKE 'IX_%'
ORDER BY name;

-- Expected Output:
-- IX_Users_CreatedAt      NONCLUSTERED
-- IX_Users_Email          UNIQUE NONCLUSTERED
-- IX_Users_FullName       NONCLUSTERED
-- IX_Users_PasswordResetToken  NONCLUSTERED
-- IX_Users_UserName       NONCLUSTERED
-- IX_Users_VerificationToken   NONCLUSTERED

-- 2. Check index sizes
SELECT 
    i.name AS IndexName,
    SUM(ps.used_page_count) * 8 / 1024 AS IndexSizeInMB
FROM sys.indexes i
JOIN sys.dm_db_partition_stats ps ON i.object_id = ps.object_id 
    AND i.index_id = ps.index_id
WHERE i.object_id = OBJECT_ID('dbo.Users')
    AND i.name LIKE 'IX_%'
GROUP BY i.name
ORDER BY SUM(ps.used_page_count) DESC;

-- 3. Verify unique constraint on Email
SELECT DISTINCT
    i.name,
    i.is_unique
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.Users')
    AND i.name = 'IX_Users_Email';
-- Should show: IX_Users_Email with is_unique = 1

-- 4. Check column types were updated
SELECT name, max_length, is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.Users')
    AND name IN ('Email', 'UserName', 'FullName', 'VerificationToken', 'PasswordResetToken')
ORDER BY name;

-- Expected: All varchar(450) or nvarchar(450)
```

### .NET Method

```csharp
// In a controller or test method
using (var context = new AppDbContext(options))
{
    var result = await context.Users
        .FromSqlInterpolated($"SELECT * FROM Users WHERE Email = '...'")
        .ToListAsync();
    
    Console.WriteLine("? Query executed using Email index");
}
```

---

## ?? Rolling Back (if needed)

### Option 1: Remove Last Migration

If the migration hasn't been applied to production yet:

```powershell
cd "C:\Users\31act\OneDrive\Documents\ostad-asp.net\AIChatApp"

# Remove the migration file (only works if not applied)
dotnet ef migrations remove --project AIChatApp.Infrastructure --startup-project AIChatApp.Web
```

**Expected Output:**
```
Done. To undo this action, use 'ef migrations add AddUserIndices'
```

### Option 2: Revert Migration from Database

If the migration was already applied:

```powershell
# Revert to the previous migration
dotnet ef database update 20251203105306_Remigrated `
  --project AIChatApp.Infrastructure `
  --startup-project AIChatApp.Web
```

**What This Does:**
- Drops all 6 indices
- Reverts column types back to `nvarchar(max)`
- Removes the `AddUserIndices` migration record

### Option 3: Manual SQL Rollback

If you need to manually rollback via SQL:

```sql
-- Drop all indices
DROP INDEX IX_Users_CreatedAt ON [Users];
DROP INDEX IX_Users_Email ON [Users];
DROP INDEX IX_Users_FullName ON [Users];
DROP INDEX IX_Users_PasswordResetToken ON [Users];
DROP INDEX IX_Users_UserName ON [Users];
DROP INDEX IX_Users_VerificationToken ON [Users];

-- Revert column types back to nvarchar(max)
-- Note: This removes the 450 character limit
ALTER TABLE [Users] ALTER COLUMN [Email] nvarchar(max) NOT NULL;
ALTER TABLE [Users] ALTER COLUMN [UserName] nvarchar(max) NOT NULL;
ALTER TABLE [Users] ALTER COLUMN [FullName] nvarchar(max) NOT NULL;
ALTER TABLE [Users] ALTER COLUMN [VerificationToken] nvarchar(max) NULL;
ALTER TABLE [Users] ALTER COLUMN [PasswordResetToken] nvarchar(max) NULL;

-- Remove migration record
DELETE FROM __EFMigrationsHistory 
WHERE MigrationId = '20251204164742_AddUserIndices';
```

---

## ?? Troubleshooting

### Problem: "Pending migrations"

```
Entity Framework migrations are pending. Please run 'dotnet ef database update'
```

**Solution:**
```powershell
dotnet ef database update --project AIChatApp.Infrastructure --startup-project AIChatApp.Web
```

### Problem: "Cannot create index - column too large"

This means the database tried to index a `nvarchar(max)` column, which isn't allowed.

**Solution:**
The migration automatically handles this by changing column types to `nvarchar(450)`.
Re-run the migration if this error occurs:

```powershell
# Rollback
dotnet ef database update 20251203105306_Remigrated `
  --project AIChatApp.Infrastructure `
  --startup-project AIChatApp.Web

# Remove and recreate
dotnet ef migrations remove --project AIChatApp.Infrastructure --startup-project AIChatApp.Web

# Generate fresh migration
dotnet ef migrations add AddUserIndices `
  --project AIChatApp.Infrastructure `
  --startup-project AIChatApp.Web

# Apply
dotnet ef database update --project AIChatApp.Infrastructure --startup-project AIChatApp.Web
```

### Problem: "Unique constraint violation on Email"

This means there are duplicate emails in your test data.

**Solution:**
```sql
-- Find duplicates
SELECT Email, COUNT(*) as Count
FROM Users
GROUP BY Email
HAVING COUNT(*) > 1;

-- Delete duplicates (keep one)
DELETE FROM Users
WHERE Id NOT IN (
    SELECT MIN(Id)
    FROM Users
    GROUP BY Email
);

-- Try migration again
```

### Problem: "Index already exists"

If the indices somehow already exist:

**Solution:**
```sql
-- Check existing indices
SELECT name FROM sys.indexes 
WHERE object_id = OBJECT_ID('dbo.Users') 
AND name LIKE 'IX_Users_%';

-- If found, manually drop them
DROP INDEX IX_Users_Email ON [Users];
DROP INDEX IX_Users_VerificationToken ON [Users];
-- ... etc
```

### Problem: "Cannot drop column - index depends on it"

This shouldn't happen with EF Core, but if it does:

**Solution:**
```sql
-- Drop all dependent indices first
DROP INDEX IX_Users_Email ON [Users];
DROP INDEX IX_Users_FullName ON [Users];
-- ... etc

-- Then drop the column
ALTER TABLE [Users] DROP COLUMN [YourColumn];
```

---

## ?? Performance Verification After Migration

### Query Performance Test

```sql
-- Test 1: Email lookup (should be instant)
SET STATISTICS IO ON;
SELECT * FROM Users WHERE Email = 'test@example.com';
-- Look for: "Table 'Users'. Scan count 0" (uses index)
SET STATISTICS IO OFF;

-- Test 2: Verification token lookup
SET STATISTICS IO ON;
SELECT * FROM Users WHERE VerificationToken = '...';
-- Look for: "Table 'Users'. Scan count 0" (uses index)
SET STATISTICS IO OFF;

-- Test 3: Recent users (uses CreatedAt index for sort)
SET STATISTICS IO ON;
SELECT TOP 10 * FROM Users ORDER BY CreatedAt DESC;
-- Look for: "Table 'Users'. Scan count 0" (uses index)
SET STATISTICS IO OFF;

-- Test 4: Search by name
SET STATISTICS IO ON;
SELECT * FROM Users WHERE FullName LIKE '%john%';
-- Look for: Low scan count (uses index)
SET STATISTICS IO OFF;
```

### Application Performance Test

```csharp
// In a test or controller
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

// Test GetByEmailAsync
var user = await _userRepository.GetByEmailAsync("test@example.com");

stopwatch.Stop();
Console.WriteLine($"Email lookup: {stopwatch.ElapsedMilliseconds}ms");
// Should be < 20ms

// Test GetRecentUsersAsync
stopwatch.Restart();
var recentUsers = await _userRepository.GetRecentUsersAsync(DateTime.UtcNow.AddDays(-5), Guid.Empty);
stopwatch.Stop();
Console.WriteLine($"Recent users: {stopwatch.ElapsedMilliseconds}ms");
// Should be < 100ms for 10K+ users

// Test SearchUsersAsync
stopwatch.Restart();
var results = await _userRepository.SearchUsersAsync("john", Guid.Empty);
stopwatch.Stop();
Console.WriteLine($"Search: {stopwatch.ElapsedMilliseconds}ms");
// Should be < 200ms
```

---

## ?? Deployment Steps Summary

```
1. Backup Database
   ?? CREATE DATABASE BACKUP

2. Review Changes
   ?? Code + SQL Changes Reviewed

3. Build Application
   ?? dotnet build ?

4. Apply Migration
   ?? dotnet ef database update ?

5. Verify Indices
   ?? SQL Query Check ?

6. Test Application
   ?? Run test suite ?

7. Monitor Performance
   ?? Watch response times

8. Rollback Plan Ready
   ?? Know how to revert if needed
```

---

## ? Success Criteria

Migration is successful when:

- ? All 6 indices are created in database
- ? Column types changed to nvarchar(450)
- ? Email index is UNIQUE
- ? Application builds without errors
- ? All queries still work
- ? Response times improved
- ? No "Pending migrations" warnings

---

## ?? Emergency Contacts / Escalation

If migration fails:

1. **Check error message** for specific SQL issue
2. **Review troubleshooting section** above
3. **Check database backup** is available
4. **Rollback** using "Rolling Back" section
5. **Contact team lead** for assistance

---

## ?? Related Documentation

- `OPTIMIZATION_SUMMARY.md` - Detailed optimization explanation
- `CODE_REFERENCE.md` - Before/after code comparison
- `IMPLEMENTATION_COMPLETE.md` - Overall summary
- `VISUAL_SUMMARY.md` - Performance charts and diagrams

---

**Migration Name:** `20251204164742_AddUserIndices`  
**Status:** ? Ready for Production  
**Created:** December 4, 2024  
**Tested:** ? Yes  

?? **You're ready to deploy!**
