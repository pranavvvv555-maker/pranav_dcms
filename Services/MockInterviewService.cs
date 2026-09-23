using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

    public byte[] GenerateMockInterviewDrivePdf(MockInterviewDrive? drive, List<MockInterviewEvaluation> evaluations, string? webRootPath = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        byte[]? nirvaaLogo = null;
        byte[]? mitLogo = null;

        try
        {
            if (!string.IsNullOrEmpty(webRootPath))
            {
                var nirvaaPath = Path.Combine(webRootPath, "images", "nirvaa.jpg");
                if (File.Exists(nirvaaPath)) nirvaaLogo = File.ReadAllBytes(nirvaaPath);

                var mitPath = Path.Combine(webRootPath, "images", "mit-wpu.jpg");
                if (File.Exists(mitPath)) mitLogo = File.ReadAllBytes(mitPath);
            }
        }
        catch
        {
            // Graceful fallback if files cannot be read from disk
        }

        var sortedEvals = evaluations
            .OrderBy(e => e.Student?.StudentCode ?? "")
            .ThenBy(e => e.Student?.FullName ?? "")
            .ToList();

        var driveTitle = drive?.Title ?? "Mock Interview Evaluation Drive";
        var subjectName = !string.IsNullOrWhiteSpace(drive?.SubjectName)
            ? drive.SubjectName
            : (drive?.Course != null ? $"{drive.Course.CourseCode} — {drive.Course.CourseName}" : "Advanced Cloud Computing & Data Storage System");
        var driveDateStr = drive?.DriveDate.ToString("dd MMMM yyyy") ?? DateTime.Now.ToString("dd MMMM yyyy");
        var statusStr = drive?.Status ?? "Completed";

        int totalCandidates = sortedEvals.Count;
        int absentCount = sortedEvals.Count(e => e.IsAbsent);
        int presentCount = sortedEvals.Count(e => !e.IsAbsent);
        var scoredList = sortedEvals.Where(e => !e.IsAbsent && e.MarksOutOf10.HasValue).Select(e => e.MarksOutOf10!.Value).ToList();
        decimal avgScore = scoredList.Any() ? Math.Round(scoredList.Average(), 1) : 0m;
        decimal minScore = scoredList.Any() ? scoredList.Min() : 0m;
        decimal maxScore = scoredList.Any() ? scoredList.Max() : 0m;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Arial"));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // Left: Nirvaa Logo & details
                        row.RelativeItem(3).Row(r =>
                        {
                            r.Spacing(6);
                            if (nirvaaLogo is not null)
                            {
                                r.AutoItem().Height(36).Image(nirvaaLogo).FitArea();
                            }
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("NIRVAA SOLUTIONS").FontSize(11).Bold().FontColor("#102A43");
                                c.Item().Text("Corporate & Industry EduTech Partner").FontSize(7.5f).FontColor("#1E5CA8");
                                c.Item().Text("Pune, Maharashtra, India").FontSize(7f).FontColor("#64748B");
                            });
                        });

                        // Center: Title & Partnership Badge
                        row.RelativeItem(5).AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Border(1).BorderColor("#FDE68A").Background("#FFFBEB").PaddingVertical(2).PaddingHorizontal(8)
                                .Text("ACADEMIC & INDUSTRY EVALUATION DESK").FontSize(7f).Bold().FontColor("#92400E");
                            c.Item().PaddingTop(2).AlignCenter().Text("M.TECH CSE · INDUSTRY MOCK INTERVIEW SCORECARD").FontSize(11.5f).Bold().FontColor("#102A43");
                            c.Item().AlignCenter().Text("Master of Technology in Computer Science & Engineering (Data Centre Systems)").FontSize(7.5f).FontColor("#475569");
                        });

                        // Right: MIT-WPU Logo & details
                        row.RelativeItem(3).AlignRight().Row(r =>
                        {
                            r.Spacing(6);
                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("MIT WORLD PEACE UNIVERSITY").FontSize(10.5f).Bold().FontColor("#102A43");
                                c.Item().Text("School of Computer Science & Tech").FontSize(7.5f).FontColor("#B45309");
                                c.Item().Text("Kothrud, Pune - 411038").FontSize(7f).FontColor("#64748B");
                            });
                            if (mitLogo is not null)
                            {
                                r.AutoItem().Height(36).Image(mitLogo).FitArea();
                            }
                        });
                    });

                    // Divider Lines
                    col.Item().PaddingTop(5).LineHorizontal(1.5f).LineColor("#1E3A8A");
                    col.Item().PaddingTop(1).LineHorizontal(0.5f).LineColor("#CBD5E1");

                    // Drive Metadata & Stats Banner
                    col.Item().PaddingTop(5).PaddingBottom(5).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(6).Row(banner =>
                    {
                        banner.RelativeItem(5).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.AutoItem().Text("Drive Title: ").Bold().FontSize(8f).FontColor("#1E3A8A");
                                r.RelativeItem().Text(driveTitle).Bold().FontSize(8.5f).FontColor("#0F172A");
                            });
                            c.Item().Row(r =>
                            {
                                r.AutoItem().Text("Subject / Course: ").Bold().FontSize(7.5f).FontColor("#475569");
                                r.RelativeItem().Text(subjectName).FontSize(7.5f).FontColor("#334155");
                            });
                            c.Item().Row(r =>
                            {
                                r.AutoItem().Text("Date & Status: ").Bold().FontSize(7.5f).FontColor("#475569");
                                r.RelativeItem().Text($"{driveDateStr}  |  Status: {statusStr}  |  Academic Year: 2026–2027").FontSize(7.5f).FontColor("#334155");
                            });
                        });

                        banner.RelativeItem(4).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Row(r =>
                            {
                                r.Spacing(4);
                                r.AutoItem().Border(1).BorderColor("#BFDBFE").Background("#EFF6FF").PaddingVertical(2).PaddingHorizontal(6)
                                    .Text($"Candidates: {totalCandidates}").Bold().FontSize(7.5f).FontColor("#1D4ED8");
                                r.AutoItem().Border(1).BorderColor("#BBF7D0").Background("#F0FDF4").PaddingVertical(2).PaddingHorizontal(6)
                                    .Text($"Evaluated: {presentCount}").Bold().FontSize(7.5f).FontColor("#15803D");
                                if (absentCount > 0)
                                {
                                    r.AutoItem().Border(1).BorderColor("#FECACA").Background("#FEF2F2").PaddingVertical(2).PaddingHorizontal(6)
                                        .Text($"Absent: {absentCount}").Bold().FontSize(7.5f).FontColor("#DC2626");
                                }
                            });
                            c.Item().PaddingTop(3).AlignRight().Row(r =>
                            {
                                r.AutoItem().Text("Cohort Average Marks: ").Bold().FontSize(7.5f).FontColor("#475569");
                                r.AutoItem().Text($"{avgScore:0.#} / 10").Bold().FontSize(8.5f).FontColor("#1E3A8A");
                                r.AutoItem().Text($"  (Min: {minScore:0.#}, Max: {maxScore:0.#})").FontSize(7.5f).FontColor("#64748B");
                            });
                        });
                    });
                });

                // Main Content Table
                page.Content().PaddingTop(2).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(22);   // Sr
                        columns.ConstantColumn(135);  // Student Name
                        columns.ConstantColumn(78);   // Roll No
                        columns.ConstantColumn(58);   // Confidence
                        columns.ConstantColumn(68);   // Communication
                        columns.ConstantColumn(58);   // Technical
                        columns.ConstantColumn(55);   // Marks / 10
                        columns.ConstantColumn(54);   // Status
                        columns.RelativeColumn(1);   // Evaluator Feedback & Remarks
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#1E3A8A").Padding(4).AlignCenter().Text("#").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#1E3A8A").Padding(4).Text("Candidate Name").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#1E3A8A").Padding(4).AlignCenter().Text("Roll / PRN").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#1E3A8A").Padding(4).AlignCenter().Text("Confidence").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#1E3A8A").Padding(4).AlignCenter().Text("Communication").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#1E3A8A").Padding(4).AlignCenter().Text("Technical").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#1E3A8A").Padding(4).AlignCenter().Text("Score / 10").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#1E3A8A").Padding(4).AlignCenter().Text("Status").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#1E3A8A").Padding(4).Text("Evaluator Feedback & Assessment Notes").FontColor(Colors.White).Bold().FontSize(7.5f);
                    });

                    int sr = 1;
                    foreach (var e in sortedEvals)
                    {
                        var isEven = (sr % 2 == 0);
                        var bg = isEven ? "#F8FAFC" : "#FFFFFF";

                        var studentName = e.Student?.FullName ?? "Unknown";
                        var rollNo = e.Student?.StudentCode ?? "";
                        var confStr = e.IsAbsent ? "—" : (e.ConfidenceScore?.ToString("0.#") ?? "—");
                        var commStr = e.IsAbsent ? "—" : (e.CommunicationScore?.ToString("0.#") ?? "—");
                        var techStr = e.IsAbsent ? "—" : (e.TechnicalScore?.ToString("0.#") ?? "—");
                        var marksStr = e.IsAbsent ? "—" : (e.MarksOutOf10?.ToString("0.#") ?? "—");
                        var statusLabel = e.IsAbsent ? "Absent" : "Completed";
                        var statusBg = e.IsAbsent ? "#FEE2E2" : "#DCFCE7";
                        var statusColor = e.IsAbsent ? "#DC2626" : "#15803D";
                        var feedbackStr = string.IsNullOrWhiteSpace(e.Feedback) ? (e.IsAbsent ? "Absent for interview." : "Good effort.") : e.Feedback;

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignCenter().Text(sr.ToString()).FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).Text(studentName).FontSize(7.5f).Bold();
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignCenter().Text(rollNo).FontSize(7f).FontColor("#475569");
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignCenter().Text(confStr).FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignCenter().Text(commStr).FontSize(7.5f);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignCenter().Text(techStr).FontSize(7.5f);

                        var marksCell = table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignCenter();
                        if (!e.IsAbsent && e.MarksOutOf10.HasValue)
                        {
                            marksCell.Text(marksStr).Bold().FontSize(8f).FontColor("#1E3A8A");
                        }
                        else
                        {
                            marksCell.Text(marksStr).FontSize(7.5f).FontColor("#94A3B8");
                        }

                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).AlignCenter().Border(1).BorderColor(statusColor).Background(statusBg).PaddingVertical(1).PaddingHorizontal(3).Text(statusLabel).FontSize(6.5f).Bold().FontColor(statusColor);
                        table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(3.5f).Text(feedbackStr).FontSize(7.5f).FontColor("#334155");

                        sr++;
                    }
                });

                // Footer
                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(8).Row(sigRow =>
                    {
                        sigRow.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(18).Text("");
                            c.Item().BorderTop(1).BorderColor("#94A3B8").PaddingTop(2).Text("Industry Evaluator / Panel Lead").FontSize(7.5f).Bold().FontColor("#1E293B");
                            c.Item().Text("Priyanka & NIRVAA Technical Panel").FontSize(7f).FontColor("#64748B");
                        });

                        sigRow.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().PaddingBottom(18).Text("");
                            c.Item().BorderTop(1).BorderColor("#94A3B8").PaddingTop(2).Text("Faculty Coordinator").FontSize(7.5f).Bold().FontColor("#1E293B");
                            c.Item().Text("Dept. of Computer Science & Engg, MIT-WPU").FontSize(7f).FontColor("#64748B");
                        });

                        sigRow.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().PaddingBottom(18).Text("");
                            c.Item().BorderTop(1).BorderColor("#94A3B8").PaddingTop(2).Text("Head of School / Director").FontSize(7.5f).Bold().FontColor("#1E293B");
                            c.Item().Text("School of Computer Science & Technology").FontSize(7f).FontColor("#64748B");
                        });
                    });

                    col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor("#E2E8F0");
                    col.Item().PaddingTop(3).Row(bottomRow =>
                    {
                        bottomRow.RelativeItem().Text($"Confidential Academic Assessment · Generated: {DateTime.Now:dd-MMM-yyyy HH:mm}").FontSize(6.5f).FontColor("#94A3B8");
                        bottomRow.RelativeItem().AlignCenter().Text("NIRVAA SOLUTIONS × MIT WORLD PEACE UNIVERSITY").FontSize(6.5f).Bold().FontColor("#64748B");
                        bottomRow.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(6.5f).FontColor("#94A3B8");
                            text.CurrentPageNumber().FontSize(6.5f).Bold().FontColor("#64748B");
                            text.Span(" of ").FontSize(6.5f).FontColor("#94A3B8");
                            text.TotalPages().FontSize(6.5f).Bold().FontColor("#64748B");
                        });
                    });
                });
            });
        });

        return doc.GeneratePdf();
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
