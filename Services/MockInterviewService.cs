using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class MockInterviewService
{
    private readonly AppDbContext _db;

    public MockInterviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MockInterviewDrive>> GetDrivesAsync(int? semesterId = null, int? courseId = null)
    {
        var query = _db.MockInterviewDrives
            .Include(d => d.Semester)
            .Include(d => d.Course)
            .Include(d => d.Evaluations)
                .ThenInclude(e => e.Interviewer)
            .AsQueryable();

        if (semesterId.HasValue && semesterId.Value > 0)
            query = query.Where(d => d.SemesterId == semesterId.Value);

        if (courseId.HasValue && courseId.Value > 0)
            query = query.Where(d => d.CourseId == courseId.Value);

        return await query.OrderByDescending(d => d.DriveDate).ToListAsync();
    }

    public async Task<MockInterviewDrive?> GetDriveWithEvaluationsAsync(int driveId)
    {
        var drive = await _db.MockInterviewDrives
            .Include(d => d.Semester)
            .Include(d => d.Course)
            .Include(d => d.Evaluations)
                .ThenInclude(e => e.Student)
            .Include(d => d.Evaluations)
                .ThenInclude(e => e.Interviewer)
            .Include(d => d.Evaluations)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(d => d.Id == driveId);

        if (drive == null) return null;

        var students = await _db.Students
            .Where(s => s.SemesterId == drive.SemesterId && s.EnrollmentStatus == "Active")
            .OrderBy(s => s.StudentCode)
            .ToListAsync();

        var existingStudentIds = drive.Evaluations.Select(e => e.StudentId).ToHashSet();
        bool addedNew = false;

        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            if (!existingStudentIds.Contains(student.Id))
            {
                var newEval = new MockInterviewEvaluation
                {
                    DriveId = drive.Id,
                    StudentId = student.Id,
                    Student = student,
                    CourseId = drive.CourseId,
                    SubjectName = drive.SubjectName,
                    StudentGroup = "",
                    Status = "Scheduled",
                    IsAbsent = false
                };
                drive.Evaluations.Add(newEval);
                _db.MockInterviewEvaluations.Add(newEval);
                addedNew = true;
            }
        }

        if (addedNew)
        {
            await _db.SaveChangesAsync();
        }

        drive.Evaluations = drive.Evaluations
            .OrderBy(e => e.Student?.StudentCode ?? "")
            .ThenBy(e => e.Student?.FullName ?? "")
            .ToList();

        return drive;
    }

    public async Task<MockInterviewDrive> CreateDriveAsync(MockInterviewDrive drive, int? leadInterviewerId = null)
    {
        _db.MockInterviewDrives.Add(drive);
        await _db.SaveChangesAsync();

        var students = await _db.Students
            .Where(s => s.SemesterId == drive.SemesterId && s.EnrollmentStatus == "Active")
            .OrderBy(s => s.StudentCode)
            .ToListAsync();

        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            _db.MockInterviewEvaluations.Add(new MockInterviewEvaluation
            {
                DriveId = drive.Id,
                StudentId = student.Id,
                CourseId = drive.CourseId,
                SubjectName = drive.SubjectName,
                InterviewerFacultyId = leadInterviewerId,
                StudentGroup = "",
                Status = drive.Status == "Completed" ? "Completed" : "Scheduled",
                IsAbsent = false
            });
        }

        await _db.SaveChangesAsync();
        return drive;
    }

    public async Task UpdateDriveAsync(MockInterviewDrive drive, int? leadInterviewerId = null)
    {
        var existing = await _db.MockInterviewDrives
            .Include(d => d.Evaluations)
            .FirstOrDefaultAsync(d => d.Id == drive.Id);
        if (existing == null) return;

        existing.Title = drive.Title.Trim();
        existing.DriveDate = drive.DriveDate;
        existing.Status = drive.Status;
        existing.PanelNotes = drive.PanelNotes?.Trim();
        existing.CourseId = drive.CourseId;
        existing.SubjectName = drive.SubjectName?.Trim();

        foreach (var eval in existing.Evaluations)
        {
            eval.CourseId = drive.CourseId;
            eval.SubjectName = drive.SubjectName?.Trim();
        }

        if (leadInterviewerId.HasValue && leadInterviewerId.Value > 0)
        {
            foreach (var eval in existing.Evaluations)
            {
                eval.InterviewerFacultyId = leadInterviewerId.Value;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task UpdateDriveSubjectAsync(int driveId, int? courseId, string? subjectName = null)
    {
        var existing = await _db.MockInterviewDrives
            .Include(d => d.Evaluations)
            .FirstOrDefaultAsync(d => d.Id == driveId);
        if (existing == null) return;

        existing.CourseId = courseId;
        if (!string.IsNullOrWhiteSpace(subjectName))
        {
            existing.SubjectName = subjectName.Trim();
        }
        else if (courseId.HasValue && courseId.Value > 0)
        {
            var course = await _db.Courses.FindAsync(courseId.Value);
            if (course != null)
            {
                existing.SubjectName = $"{course.CourseCode} — {course.CourseName}";
            }
        }
        else
        {
            existing.SubjectName = null;
        }

        foreach (var eval in existing.Evaluations)
        {
            eval.CourseId = existing.CourseId;
            eval.SubjectName = existing.SubjectName;
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteDriveAsync(int driveId)
    {
        var drive = await _db.MockInterviewDrives
            .Include(d => d.Evaluations)
            .FirstOrDefaultAsync(d => d.Id == driveId);
        if (drive != null)
        {
            _db.MockInterviewEvaluations.RemoveRange(drive.Evaluations);
            _db.MockInterviewDrives.Remove(drive);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SaveEvaluationAsync(MockInterviewEvaluation evaluation)
    {
        var existing = await _db.MockInterviewEvaluations.FindAsync(evaluation.Id);
        if (existing == null) return;

        existing.InterviewerFacultyId = evaluation.InterviewerFacultyId;
        existing.StudentGroup = evaluation.StudentGroup;
        existing.IsAbsent = evaluation.IsAbsent;

        if (evaluation.IsAbsent)
        {
            existing.ConfidenceScore = null;
            existing.CommunicationScore = null;
            existing.TechnicalScore = null;
            existing.MarksOutOf10 = null;
            existing.Status = "Absent";
        }
        else
        {
            existing.ConfidenceScore = evaluation.ConfidenceScore.HasValue ? Math.Clamp(evaluation.ConfidenceScore.Value, 0, 10) : null;
            existing.CommunicationScore = evaluation.CommunicationScore.HasValue ? Math.Clamp(evaluation.CommunicationScore.Value, 0, 10) : null;
            existing.TechnicalScore = evaluation.TechnicalScore.HasValue ? Math.Clamp(evaluation.TechnicalScore.Value, 0, 10) : null;
            existing.MarksOutOf10 = evaluation.MarksOutOf10.HasValue ? Math.Clamp(evaluation.MarksOutOf10.Value, 0, 10) : null;
            existing.Status = "Completed";
        }

        existing.Feedback = evaluation.Feedback?.Trim();
        existing.InterviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task SaveEvaluationsBatchAsync(IEnumerable<MockInterviewEvaluation> evaluations)
    {
        var evalList = evaluations.ToList();
        var ids = evalList.Select(e => e.Id).ToList();
        var existingEntities = await _db.MockInterviewEvaluations
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id);

        foreach (var eval in evalList)
        {
            if (!existingEntities.TryGetValue(eval.Id, out var entity)) continue;

            entity.StudentGroup = string.IsNullOrWhiteSpace(eval.StudentGroup) ? "Group A" : eval.StudentGroup.Trim();
            entity.IsAbsent = eval.IsAbsent;

            if (eval.IsAbsent)
            {
                entity.ConfidenceScore = null;
                entity.CommunicationScore = null;
                entity.TechnicalScore = null;
                entity.MarksOutOf10 = null;
                entity.Status = "Absent";
                entity.Feedback = string.IsNullOrWhiteSpace(eval.Feedback) ? "absent" : eval.Feedback.Trim();
            }
            else
            {
                entity.ConfidenceScore = eval.ConfidenceScore.HasValue ? Math.Clamp(eval.ConfidenceScore.Value, 0, 10) : null;
                entity.CommunicationScore = eval.CommunicationScore.HasValue ? Math.Clamp(eval.CommunicationScore.Value, 0, 10) : null;
                entity.TechnicalScore = eval.TechnicalScore.HasValue ? Math.Clamp(eval.TechnicalScore.Value, 0, 10) : null;
                entity.MarksOutOf10 = eval.MarksOutOf10.HasValue ? Math.Clamp(eval.MarksOutOf10.Value, 0, 10) : null;
                entity.Status = "Completed";
                entity.Feedback = eval.Feedback?.Trim();
            }

            entity.InterviewedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<Student>> GetAvailableStudentsForDriveAsync(int driveId)
    {
        var drive = await _db.MockInterviewDrives.FindAsync(driveId);
        if (drive == null) return new List<Student>();

        var existingStudentIds = await _db.MockInterviewEvaluations
            .Where(e => e.DriveId == driveId)
            .Select(e => e.StudentId)
            .ToListAsync();

        return await _db.Students
            .Where(s => s.SemesterId == drive.SemesterId && !existingStudentIds.Contains(s.Id))
            .OrderBy(s => s.FullName)
            .ToListAsync();
    }

    public async Task<MockInterviewEvaluation> AddStudentToDriveAsync(int driveId, int studentId, string groupName = "")
    {
        var student = await _db.Students.FindAsync(studentId);
        if (student == null) throw new InvalidOperationException("Student not found");

        var drive = await _db.MockInterviewDrives.FindAsync(driveId);
        var evaluation = new MockInterviewEvaluation
        {
            DriveId = driveId,
            StudentId = studentId,
            Student = student,
            CourseId = drive?.CourseId,
            SubjectName = drive?.SubjectName,
            StudentGroup = groupName,
            Status = "Scheduled",
            IsAbsent = false
        };

        _db.MockInterviewEvaluations.Add(evaluation);
        await _db.SaveChangesAsync();
        return evaluation;
    }

    public async Task RemoveEvaluationAsync(int evaluationId)
    {
        var evaluation = await _db.MockInterviewEvaluations.FindAsync(evaluationId);
        if (evaluation != null)
        {
            _db.MockInterviewEvaluations.Remove(evaluation);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<MockInterviewReadinessStats> GetOverallReadinessStatsAsync(int? driveId = null)
    {
        var query = _db.MockInterviewEvaluations.AsQueryable();
        if (driveId.HasValue && driveId.Value > 0)
            query = query.Where(e => e.DriveId == driveId.Value);

        var evaluations = await query.ToListAsync();

        int total = evaluations.Count;
        int absent = evaluations.Count(e => e.IsAbsent);
        var scored = evaluations.Where(e => !e.IsAbsent && e.MarksOutOf10.HasValue).ToList();

        decimal avgMarks = scored.Any() ? scored.Average(e => e.MarksOutOf10!.Value) : 0;
        decimal avgConf = scored.Where(e => e.ConfidenceScore.HasValue).Select(e => e.ConfidenceScore!.Value).DefaultIfEmpty(0).Average();
        decimal avgComm = scored.Where(e => e.CommunicationScore.HasValue).Select(e => e.CommunicationScore!.Value).DefaultIfEmpty(0).Average();
        decimal avgTech = scored.Where(e => e.TechnicalScore.HasValue).Select(e => e.TechnicalScore!.Value).DefaultIfEmpty(0).Average();

        int highScorers = scored.Count(e => e.MarksOutOf10 >= 8.5m);
        int mediumScorers = scored.Count(e => e.MarksOutOf10 >= 6.0m && e.MarksOutOf10 < 8.5m);
        int lowScorers = scored.Count(e => e.MarksOutOf10 < 6.0m);

        return new MockInterviewReadinessStats
        {
            TotalEvaluated = scored.Count,
            AbsentCount = absent,
            AverageMarksOutOf10 = Math.Round(avgMarks, 1),
            AverageConfidence = Math.Round(avgConf, 1),
            AverageCommunication = Math.Round(avgComm, 1),
            AverageTechnical = Math.Round(avgTech, 1),
            HighScorersCount = highScorers,
            MediumScorersCount = mediumScorers,
            LowScorersCount = lowScorers
        };
    }
}

public class MockInterviewReadinessStats
{
    public int TotalEvaluated { get; set; }
    public int AbsentCount { get; set; }
    public decimal AverageMarksOutOf10 { get; set; }
    public decimal AverageConfidence { get; set; }
    public decimal AverageCommunication { get; set; }
    public decimal AverageTechnical { get; set; }
    public int HighScorersCount { get; set; }
    public int MediumScorersCount { get; set; }
    public int LowScorersCount { get; set; }
}
