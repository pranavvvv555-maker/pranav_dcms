using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class DashboardService(AppDbContext db)
{
    public async Task<int> GetActiveFacultyCountAsync()
        => await db.Faculties.CountAsync(f => f.IsActive);

    public async Task<int> GetStudentCountAsync()
        => await db.Students.CountAsync(s => s.EnrollmentStatus == "Active");

    public async Task<int> GetTotalClassesThisMonthAsync()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        return await db.Sessions
            .Where(s => s.Date >= startOfMonth && s.Date <= endOfMonth
                && s.Status == SessionStatus.Conducted && s.IsApproved)
            .CountAsync();
    }

    public async Task<decimal> GetTotalPaymentDueThisMonthAsync()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var sessions = await db.Sessions
            .Where(s => s.Date >= startOfMonth && s.Date <= endOfMonth
                && s.Status == SessionStatus.Conducted && s.IsApproved)
            .ToListAsync();

        decimal total = 0;
        foreach (var group in sessions.GroupBy(s => s.FacultyId))
        {
            var rate = await db.FacultyRates
                .Where(r => r.FacultyId == group.Key
                    && r.EffectiveFrom <= today
                    && (r.EffectiveTo == null || r.EffectiveTo > today))
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            var lectureRate = rate is null ? 0 : rate.LectureRateINR > 0 ? rate.LectureRateINR : rate.HourlyRateINR;
            var practicalRate = rate is null ? 0 : rate.PracticalRateINR > 0 ? rate.PracticalRateINR : rate.HourlyRateINR;
            var lectures = group.Count(s => s.SessionType is "Lecture" or "Tutorial");
            var practicals = group.Count() - lectures;
            total += (lectures * lectureRate) + (practicals * practicalRate);
        }

        return total;
    }

    public async Task<List<(string FacultyName, decimal Hours)>> GetFacultyHoursThisMonthAsync()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var data = await db.Sessions
            .Include(s => s.Faculty)
            .Where(s => s.Date >= startOfMonth && s.Date <= endOfMonth
                && s.Status == SessionStatus.Conducted)
            .GroupBy(s => s.Faculty.FullName)
            .Select(g => new { Name = g.Key, Hours = g.Sum(s => s.DurationHours) })
            .OrderByDescending(x => x.Hours)
            .ToListAsync();

        return data.Select(d => (d.Name, d.Hours)).ToList();
    }

    public async Task<List<Session>> GetRecentSessionsAsync(int count = 10)
        => await db.Sessions
            .Include(s => s.Faculty)
            .Include(s => s.Course)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<int> GetCourseCountAsync()
        => await db.Courses.CountAsync();
}
