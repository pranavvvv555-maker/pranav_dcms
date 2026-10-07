using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class AcademicHolidayService(AppDbContext db)
{
    private static bool _tableEnsured = false;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private static readonly (DateTime Date, string Name, string Description)[] DefaultHolidays = new[]
    {
        (new DateTime(2026, 9, 14), "Ganesh Chaturthi", "State and University Holiday"),
        (new DateTime(2026, 9, 18), "Gouri Poojan", "University and State Gazetted Holiday"),
        (new DateTime(2026, 9, 25), "Anant Chaturdashi", "University and State Gazetted Holiday"),
        (new DateTime(2026, 10, 2), "Gandhi Jayanti", "National Gazetted Holiday")
    };

    public async Task EnsureTableExistsAsync()
    {
        if (_tableEnsured) return;
        await _initLock.WaitAsync();
        try
        {
            if (_tableEnsured) return;

            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS AcademicHolidays (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        Description TEXT NULL,
                        IsRemovedDay INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL
                    );
                ");
                await db.Database.ExecuteSqlRawAsync(@"
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_AcademicHolidays_Date ON AcademicHolidays (Date);
                ");
            }
            catch
            {
                // Ignore if already created
            }

            // Seed default holidays if table is empty
            var count = await db.AcademicHolidays.CountAsync();
            if (count == 0)
            {
                foreach (var (date, name, desc) in DefaultHolidays)
                {
                    db.AcademicHolidays.Add(new AcademicHoliday
                    {
                        Date = date.Date,
                        Name = name,
                        Description = desc,
                        IsRemovedDay = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await db.SaveChangesAsync();
            }

            _tableEnsured = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<AcademicHoliday>> GetAllAsync()
    {
        await EnsureTableExistsAsync();
        return await db.AcademicHolidays.OrderBy(h => h.Date).ToListAsync();
    }

    public async Task<Dictionary<DateTime, string>> GetHolidaysDictAsync()
    {
        await EnsureTableExistsAsync();
        var list = await db.AcademicHolidays.ToListAsync();
        return list.ToDictionary(h => h.Date.Date, h => h.Name);
    }

    public async Task<AcademicHoliday?> GetHolidayAsync(DateTime date)
    {
        await EnsureTableExistsAsync();
        return await db.AcademicHolidays.FirstOrDefaultAsync(h => h.Date.Date == date.Date);
    }

    public async Task<bool> IsHolidayAsync(DateTime date)
    {
        await EnsureTableExistsAsync();
        return await db.AcademicHolidays.AnyAsync(h => h.Date.Date == date.Date);
    }

    public async Task<string?> GetHolidayNameAsync(DateTime date)
    {
        await EnsureTableExistsAsync();
        var h = await db.AcademicHolidays.FirstOrDefaultAsync(h => h.Date.Date == date.Date);
        return h?.Name;
    }

    public async Task<AcademicHoliday> DeclareHolidayAsync(
        DateTime date,
        string name,
        string? description = null,
        bool isRemovedDay = false,
        bool clearScheduledClasses = true,
        bool removeOnlineToo = false)
    {
        await EnsureTableExistsAsync();
        var existing = await db.AcademicHolidays.FirstOrDefaultAsync(h => h.Date.Date == date.Date);
        if (existing != null)
        {
            existing.Name = string.IsNullOrWhiteSpace(name) ? (isRemovedDay ? "Removed Day" : "Declared Holiday") : name.Trim();
            existing.Description = description?.Trim();
            existing.IsRemovedDay = isRemovedDay;
        }
        else
        {
            existing = new AcademicHoliday
            {
                Date = date.Date,
                Name = string.IsNullOrWhiteSpace(name) ? (isRemovedDay ? "Removed Day" : "Declared Holiday") : name.Trim(),
                Description = description?.Trim(),
                IsRemovedDay = isRemovedDay,
                CreatedAt = DateTime.UtcNow
            };
            db.AcademicHolidays.Add(existing);
        }

        if (clearScheduledClasses)
        {
            var query = db.Sessions.Where(s => s.Date.Date == date.Date);
            if (!removeOnlineToo)
            {
                query = query.Where(s => !s.IsOnline);
            }

            var sessionsToRemove = await query.ToListAsync();
            if (sessionsToRemove.Count > 0)
            {
                var sIds = sessionsToRemove.Select(s => s.Id).ToList();
                var attendances = await db.SessionAttendances.Where(a => sIds.Contains(a.SessionId)).ToListAsync();
                if (attendances.Count > 0)
                {
                    db.SessionAttendances.RemoveRange(attendances);
                }
                db.Sessions.RemoveRange(sessionsToRemove);
            }
        }

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> RevokeHolidayAsync(DateTime date)
    {
        await EnsureTableExistsAsync();
        var existing = await db.AcademicHolidays.FirstOrDefaultAsync(h => h.Date.Date == date.Date);
        if (existing != null)
        {
            db.AcademicHolidays.Remove(existing);
            await db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<int> RemoveDaySessionsAsync(DateTime date, bool removeOnlineToo = false)
    {
        var query = db.Sessions.Where(s => s.Date.Date == date.Date);
        if (!removeOnlineToo)
        {
            query = query.Where(s => !s.IsOnline);
        }

        var sessionsToRemove = await query.ToListAsync();
        if (sessionsToRemove.Count > 0)
        {
            var sIds = sessionsToRemove.Select(s => s.Id).ToList();
            var attendances = await db.SessionAttendances.Where(a => sIds.Contains(a.SessionId)).ToListAsync();
            if (attendances.Count > 0)
            {
                db.SessionAttendances.RemoveRange(attendances);
            }
            db.Sessions.RemoveRange(sessionsToRemove);
            await db.SaveChangesAsync();
        }
        return sessionsToRemove.Count;
    }
}
