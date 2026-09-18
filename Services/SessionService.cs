using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class SessionService(AppDbContext db)
{
    public async Task<List<Session>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null,
        int? facultyId = null, int? courseId = null)
    {
        var query = db.Sessions
            .Include(s => s.Faculty)
            .Include(s => s.Course)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(s => s.Date >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(s => s.Date <= toDate.Value);
        if (facultyId.HasValue)
            query = query.Where(s => s.FacultyId == facultyId.Value);
        if (courseId.HasValue)
            query = query.Where(s => s.CourseId == courseId.Value);

        var list = await query.OrderByDescending(s => s.Date).ToListAsync();
        return list.OrderByDescending(s => s.Date).ThenBy(s => s.ActualStartTime).ToList();
    }

    public async Task<Session?> GetByIdAsync(int id)
        => await db.Sessions
            .Include(s => s.Faculty)
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);

    private async Task ValidateSessionAsync(Session session)
    {
        if (session.ActualStartTime < TimeSpan.Zero || session.ActualEndTime > TimeSpan.FromDays(1) || session.ActualEndTime <= session.ActualStartTime)
            throw new InvalidOperationException("End time must be later than start time within the same day.");
        var candidates = await db.Sessions.Where(s => s.Id != session.Id && s.Date.Date == session.Date.Date && (s.FacultyId == session.FacultyId || s.CourseId == session.CourseId)).ToListAsync();
        if (candidates.Any(s => s.ActualStartTime < session.ActualEndTime && s.ActualEndTime > session.ActualStartTime))
            throw new InvalidOperationException("This faculty member or course already has an overlapping session.");
    }

    public async Task<Session> LogSessionAsync(Session session)
    {
        await ValidateSessionAsync(session);
        session.DurationHours = (decimal)(session.ActualEndTime - session.ActualStartTime).TotalHours;

        session.Status = SessionStatus.Conducted;
        session.CreatedAt = DateTime.UtcNow;

        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task<Session> ScheduleSessionAsync(Session session)
    {
        await ValidateSessionAsync(session);
        session.DurationHours = (decimal)(session.ActualEndTime - session.ActualStartTime).TotalHours;

        session.Status = SessionStatus.Scheduled;
        session.IsApproved = false;
        session.CreatedAt = DateTime.UtcNow;

        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public async Task MarkConductedAsync(int sessionId)
    {
        var session = await db.Sessions.FindAsync(sessionId);
        if (session is null) return;

        session.Status = SessionStatus.Conducted;
        session.IsApproved = true;
        session.ApprovedBy = "Administrator";
        session.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<int> SubmitCompletedDayAsync(DateTime date)
    {
        var plannedSessions = await db.Sessions
            .Where(session => session.Date.Date == date.Date && session.Status == SessionStatus.Scheduled)
            .ToListAsync();

        foreach (var session in plannedSessions)
        {
            session.Status = SessionStatus.Conducted;
            session.IsApproved = true;
            session.ApprovedBy = "Administrator";
            session.ApprovedAt = DateTime.UtcNow;
        }

        if (plannedSessions.Count > 0)
            await db.SaveChangesAsync();

        return plannedSessions.Count;
    }

    public async Task UpdateAsync(Session session)
    {
        await ValidateSessionAsync(session);
        session.DurationHours = (decimal)(session.ActualEndTime - session.ActualStartTime).TotalHours;

        db.Sessions.Update(session);
        await db.SaveChangesAsync();
    }

    public async Task ApproveAsync(int sessionId, string approvedBy)
    {
        var session = await db.Sessions.FindAsync(sessionId);
        if (session is not null)
        {
            session.IsApproved = true;
            session.ApprovedBy = approvedBy;
            session.ApprovedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task CancelAsync(int sessionId, string reason)
    {
        var session = await db.Sessions.FindAsync(sessionId);
        if (session is not null)
        {
            session.Status = SessionStatus.Cancelled;
            session.CancellationReason = reason;
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var session = await db.Sessions.FindAsync(id);
        if (session is not null)
        {
            db.Sessions.Remove(session);
            await db.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetTotalHoursAsync(int facultyId, DateTime from, DateTime to)
        => await db.Sessions
            .Where(s => s.FacultyId == facultyId
                && s.Date >= from && s.Date <= to
                && s.Status == SessionStatus.Conducted)
            .SumAsync(s => s.DurationHours);

    public async Task<(int LectureCount, int PracticalCount)> GetClassCountsAsync(int facultyId, DateTime from, DateTime to)
    {
        var sessionTypes = await db.Sessions
            .Where(s => s.FacultyId == facultyId
                && s.Date >= from && s.Date <= to
                && s.Status == SessionStatus.Conducted
                && s.IsApproved)
            .Select(s => s.SessionType)
            .ToListAsync();

        var lectureCount = sessionTypes.Count(type => type is "Lecture" or "Tutorial");
        return (lectureCount, sessionTypes.Count - lectureCount);
    }
}
