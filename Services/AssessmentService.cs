using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class AssessmentService
{
    private readonly AppDbContext _db;

    public AssessmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Assessment>> GetAssessmentsAsync(int? courseId = null, string? type = null)
    {
        var query = _db.Assessments
            .Include(a => a.Course)
            .Include(a => a.Semester)
            .Include(a => a.Marks)
            .AsQueryable();

        if (courseId.HasValue && courseId.Value > 0)
            query = query.Where(a => a.CourseId == courseId.Value);

        if (!string.IsNullOrWhiteSpace(type) && type != "All")
            query = query.Where(a => a.Type == type);

        return await query.OrderByDescending(a => a.AssessmentDate).ToListAsync();
    }

    public async Task<Assessment?> GetAssessmentWithMarksAsync(int assessmentId)
    {
        var assessment = await _db.Assessments
            .Include(a => a.Course)
            .Include(a => a.Semester)
            .Include(a => a.Marks)
                .ThenInclude(m => m.Student)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);

        if (assessment == null) return null;

        // Ensure all active semester students have a mark entry
        var students = await _db.Students
            .Where(s => s.SemesterId == assessment.SemesterId && s.EnrollmentStatus == "Active")
            .OrderBy(s => s.StudentCode)
            .ToListAsync();

        var existingStudentIds = assessment.Marks.Select(m => m.StudentId).ToHashSet();
        bool addedNew = false;

        foreach (var student in students)
        {
            if (!existingStudentIds.Contains(student.Id))
            {
                var newMark = new StudentAssessmentMark
                {
                    AssessmentId = assessment.Id,
                    StudentId = student.Id,
                    Student = student,
                    MarksObtained = null,
                    IsAbsent = false
                };
                assessment.Marks.Add(newMark);
                _db.StudentAssessmentMarks.Add(newMark);
                addedNew = true;
            }
        }

        if (addedNew)
        {
            await _db.SaveChangesAsync();
        }

        assessment.Marks = assessment.Marks.OrderBy(m => m.Student?.StudentCode ?? "").ToList();
        return assessment;
    }

    public async Task<Assessment> CreateAssessmentAsync(Assessment assessment)
    {
        _db.Assessments.Add(assessment);
        await _db.SaveChangesAsync();

        // Populate empty marks for active students
        var students = await _db.Students
            .Where(s => s.SemesterId == assessment.SemesterId && s.EnrollmentStatus == "Active")
            .OrderBy(s => s.StudentCode)
            .ToListAsync();

        foreach (var student in students)
        {
            _db.StudentAssessmentMarks.Add(new StudentAssessmentMark
            {
                AssessmentId = assessment.Id,
                StudentId = student.Id,
                MarksObtained = null,
                IsAbsent = false
            });
        }

        await _db.SaveChangesAsync();
        return assessment;
    }

    public async Task UpdateAssessmentAsync(Assessment assessment)
    {
        var existing = await _db.Assessments.FindAsync(assessment.Id);
        if (existing == null) return;

        existing.Title = assessment.Title;
        existing.Type = assessment.Type;
        existing.MaxMarks = assessment.MaxMarks;
        existing.WeightagePercent = assessment.WeightagePercent;
        existing.AssessmentDate = assessment.AssessmentDate;
        existing.Description = assessment.Description;
        existing.Status = assessment.Status;
        existing.CourseId = assessment.CourseId;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAssessmentAsync(int assessmentId)
    {
        var assessment = await _db.Assessments.FindAsync(assessmentId);
        if (assessment != null)
        {
            _db.Assessments.Remove(assessment);
            await _db.SaveChangesAsync();
        }
    }

    public async Task SaveMarksAsync(int assessmentId, List<StudentAssessmentMark> marks)
    {
        var assessment = await _db.Assessments.FindAsync(assessmentId);
        if (assessment == null) return;

        foreach (var mark in marks)
        {
            var existing = await _db.StudentAssessmentMarks.FindAsync(mark.Id);
            if (existing != null)
            {
                existing.MarksObtained = mark.IsAbsent ? 0 : mark.MarksObtained;
                existing.IsAbsent = mark.IsAbsent;
                existing.Remarks = mark.Remarks;
                existing.GradedAt = DateTime.UtcNow;
            }
        }

        int gradedCount = marks.Count(m => m.MarksObtained.HasValue || m.IsAbsent);
        if (gradedCount > 0 && assessment.Status == "Scheduled")
        {
            assessment.Status = "Evaluated";
        }

        await _db.SaveChangesAsync();
    }

    public async Task<AssessmentOverallStats> GetOverallStatsAsync()
    {
        var assessments = await _db.Assessments.Include(a => a.Marks).ToListAsync();
        var allMarks = await _db.StudentAssessmentMarks
            .Include(m => m.Assessment)
            .Where(m => m.MarksObtained.HasValue && !m.IsAbsent && m.Assessment.MaxMarks > 0)
            .ToListAsync();

        int totalCount = assessments.Count;
        int evaluatedCount = assessments.Count(a => a.Marks.Any(m => m.MarksObtained.HasValue || m.IsAbsent));

        decimal overallAveragePct = 0;
        if (allMarks.Any())
        {
            overallAveragePct = allMarks.Average(m => (m.MarksObtained!.Value / m.Assessment.MaxMarks) * 100);
        }

        int passingMarksCount = allMarks.Count(m => (m.MarksObtained!.Value / m.Assessment.MaxMarks) >= 0.40m);
        decimal passRate = allMarks.Any() ? ((decimal)passingMarksCount / allMarks.Count) * 100 : 100;

        return new AssessmentOverallStats
        {
            TotalAssessments = totalCount,
            EvaluatedAssessments = evaluatedCount,
            AverageScorePercent = Math.Round(overallAveragePct, 1),
            PassRatePercent = Math.Round(passRate, 1)
        };
    }

    public async Task<CourseGradeMatrix> GetCourseGradeMatrixAsync(int courseId)
    {
        var course = await _db.Courses
            .Include(c => c.Semester)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) return new CourseGradeMatrix();

        var assessments = await _db.Assessments
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.AssessmentDate)
            .ToListAsync();

        var students = await _db.Students
            .Where(s => s.SemesterId == course.SemesterId && s.EnrollmentStatus == "Active")
            .OrderBy(s => s.StudentCode)
            .ToListAsync();

        var assessmentIds = assessments.Select(a => a.Id).ToList();
        var marks = await _db.StudentAssessmentMarks
            .Where(m => assessmentIds.Contains(m.AssessmentId))
            .ToListAsync();

        var rows = new List<StudentGradeRow>();

        foreach (var student in students)
        {
            var studentMarks = marks.Where(m => m.StudentId == student.Id).ToList();
            var row = new StudentGradeRow
            {
                StudentId = student.Id,
                StudentCode = student.StudentCode,
                FullName = student.FullName
            };

            decimal totalObtained = 0;
            decimal totalMax = 0;

            foreach (var a in assessments)
            {
                var mark = studentMarks.FirstOrDefault(m => m.AssessmentId == a.Id);
                decimal? score = mark?.IsAbsent == true ? 0 : mark?.MarksObtained;
                row.AssessmentScores[a.Id] = score;

                if (score.HasValue)
                {
                    totalObtained += score.Value;
                    totalMax += a.MaxMarks;
                }
            }

            row.TotalObtained = totalObtained;
            row.TotalMaxMarks = totalMax;
            row.Percentage = totalMax > 0 ? Math.Round((totalObtained / totalMax) * 100, 1) : 0;
            row.Grade = CalculateGrade(row.Percentage);

            rows.Add(row);
        }

        return new CourseGradeMatrix
        {
            Course = course,
            Assessments = assessments,
            StudentRows = rows
        };
    }

    public static string CalculateGrade(decimal percentage)
    {
        return percentage switch
        {
            >= 90 => "O (Outstanding)",
            >= 80 => "A+ (Excellent)",
            >= 70 => "A (Very Good)",
            >= 60 => "B+ (Good)",
            >= 50 => "B (Above Average)",
            >= 40 => "C (Pass)",
            _ => "F (Fail)"
        };
    }
}

public class AssessmentOverallStats
{
    public int TotalAssessments { get; set; }
    public int EvaluatedAssessments { get; set; }
    public decimal AverageScorePercent { get; set; }
    public decimal PassRatePercent { get; set; }
}

public class CourseGradeMatrix
{
    public Course? Course { get; set; }
    public List<Assessment> Assessments { get; set; } = new();
    public List<StudentGradeRow> StudentRows { get; set; } = new();
}

public class StudentGradeRow
{
    public int StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Dictionary<int, decimal?> AssessmentScores { get; set; } = new();
    public decimal TotalObtained { get; set; }
    public decimal TotalMaxMarks { get; set; }
    public decimal Percentage { get; set; }
    public string Grade { get; set; } = "—";
}
