using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class AttendanceService(AppDbContext db)
{
    public async Task<List<DailySessionAttendanceSummary>> GetDailySessionSummariesAsync(DateTime date)
    {
        var activeStudents = await db.Students.Where(student => student.EnrollmentStatus == "Active").ToListAsync();
        var sessions = await db.Sessions
            .Include(session => session.Faculty)
            .Include(session => session.Course)
            .Include(session => session.AttendanceRecords)
            .Where(session => session.Date.Date == date.Date && session.Status == SessionStatus.Conducted)
            .ToListAsync();

        return sessions
            // SQLite cannot order TimeSpan values. Sessions for one date are
            // few, so ordering the materialised list is safe and reliable.
            .OrderBy(session => session.ActualStartTime)
            .ThenBy(session => session.Faculty.FullName)
            .Select(session => new DailySessionAttendanceSummary(
            session.Id,
            session.Date,
            session.Faculty.FullName,
            session.Course.CourseName,
            session.SessionType,
            session.AttendanceRecords.Count(record => record.IsPresent),
            session.AttendanceRecords.Any() ? session.AttendanceRecords.Count : activeStudents.Count(s => s.SemesterId == session.Course.SemesterId),
            session.AttendanceRecords.Any()))
            .ToList();
    }

    public async Task<List<AttendanceStudentItem>> GetAttendanceChecklistAsync(int sessionId)
    {
        var session = await db.Sessions.Include(s => s.Course).FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("Session not found.");
        var attendance = await db.SessionAttendances
            .Where(record => record.SessionId == sessionId)
            .ToDictionaryAsync(record => record.StudentId, record => record.IsPresent);
        var hasSavedAttendance = attendance.Count > 0;

        var activeStudents = await db.Students
            .Where(student => hasSavedAttendance ? attendance.Keys.Contains(student.Id) : student.EnrollmentStatus == "Active" && student.SemesterId == session.Course.SemesterId)
            .OrderBy(student => student.FullName)
            .Select(student => new
            {
                student.Id,
                student.StudentCode,
                student.FullName
            })
            .ToListAsync();

        return activeStudents
            .Select(student => new AttendanceStudentItem(
                student.Id,
                student.StudentCode,
                student.FullName,
                hasSavedAttendance ? attendance.GetValueOrDefault(student.Id) : true))
            .ToList();
    }

    public async Task SaveAttendanceAsync(int sessionId, IEnumerable<AttendanceStudentItem> students)
    {
        var sessionExists = await db.Sessions.AnyAsync(session => session.Id == sessionId);
        if (!sessionExists) return;

        var existing = await db.SessionAttendances
            .Where(record => record.SessionId == sessionId)
            .ToListAsync();
        db.SessionAttendances.RemoveRange(existing);

        var roster = await GetAttendanceChecklistAsync(sessionId);
        var activeStudentIds = roster.Select(s => s.StudentId).ToHashSet();
        var submitted = students
            .Where(student => activeStudentIds.Contains(student.StudentId))
            .GroupBy(student => student.StudentId)
            .Select(group => group.Last())
            .ToList();

        if (submitted.Count != activeStudentIds.Count) throw new InvalidOperationException("Submit the complete class roster.");
        db.SessionAttendances.AddRange(submitted.Select(student => new SessionAttendance
        {
            SessionId = sessionId,
            StudentId = student.StudentId,
            IsPresent = student.IsPresent,
            RecordedAt = DateTime.UtcNow
        }));

        await db.SaveChangesAsync();
    }

    public async Task<List<FacultyNightReportRow>> GetFacultyNightReportAsync(DateTime date)
    {
        var sessions = await db.Sessions
            .Include(session => session.Faculty)
            .Include(session => session.AttendanceRecords)
            .Where(session => session.Date.Date == date.Date && session.Status == SessionStatus.Conducted)
            .ToListAsync();

        return sessions
            .GroupBy(session => new { session.FacultyId, session.Faculty.FullName })
            .Select(group => new FacultyNightReportRow(
                group.Key.FacultyId,
                group.Key.FullName,
                group.Count(session => IsLecture(session.SessionType)),
                group.Count(session => !IsLecture(session.SessionType)),
                group.Where(session => IsLecture(session.SessionType))
                    .Sum(session => session.AttendanceRecords.Count(record => record.IsPresent)),
                group.Where(session => !IsLecture(session.SessionType))
                    .Sum(session => session.AttendanceRecords.Count(record => record.IsPresent)),
                group.Count(session => session.AttendanceRecords.Any())))
            .OrderBy(row => row.FacultyName)
            .ToList();
    }

    private static bool IsLecture(string sessionType) => sessionType is "Lecture" or "Tutorial";
}

public sealed record DailySessionAttendanceSummary(
    int SessionId,
    DateTime Date,
    string FacultyName,
    string CourseName,
    string SessionType,
    int PresentCount,
    int StudentCount,
    bool IsAttendanceMarked);

public sealed class AttendanceStudentItem(int studentId, string studentCode, string fullName, bool isPresent)
{
    public int StudentId { get; } = studentId;
    public string StudentCode { get; } = studentCode;
    public string FullName { get; } = fullName;
    public bool IsPresent { get; set; } = isPresent;
}

public sealed record FacultyNightReportRow(
    int FacultyId,
    string FacultyName,
    int LectureCount,
    int PracticalCount,
    int LectureAttendance,
    int PracticalAttendance,
    int SessionsWithAttendance)
{
    public int TotalAttendance => LectureAttendance + PracticalAttendance;
}
