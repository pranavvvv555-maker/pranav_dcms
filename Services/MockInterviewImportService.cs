using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class MockInterviewImportService
{
    private readonly AppDbContext _db;

    public MockInterviewImportService(AppDbContext db)
    {
        _db = db;
    }

    public class MockImportColumnMapping
    {
        public int NameColIndex { get; set; } = -1;
        public int RollColIndex { get; set; } = -1;
        public int ConfColIndex { get; set; } = -1;
        public int CommColIndex { get; set; } = -1;
        public int TechColIndex { get; set; } = -1;
        public int MarksColIndex { get; set; } = -1;
        public int FeedbackColIndex { get; set; } = -1;
        public int AbsentColIndex { get; set; } = -1;

        public List<string> DetectedColumns { get; set; } = new();
        public List<string> HandledOmissions { get; set; } = new();
    }

    public class MockImportRow
    {
        public int RowIndex { get; set; }
        public string RawName { get; set; } = string.Empty;
        public string RawRoll { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
        public decimal? Communication { get; set; }
        public decimal? Technical { get; set; }
        public decimal? Marks { get; set; }
        public string? Feedback { get; set; }
        public bool IsAbsent { get; set; }

        public int? MatchedStudentId { get; set; }
        public string? MatchedStudentName { get; set; }
        public string? MatchedStudentRoll { get; set; }
        public string MatchStatus { get; set; } = "Unmatched"; // "Matched Drive", "Matched Semester", "New Student", "Skipped"
        public bool IsSelected { get; set; } = true;
    }

    public class MockImportParseResult
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRowsRead { get; set; }
        public int ValidStudentsCount => Rows.Count(r => r.MatchStatus != "Skipped" && (!string.IsNullOrWhiteSpace(r.RawName) || !string.IsNullOrWhiteSpace(r.RawRoll)));
        public int MatchedCount => Rows.Count(r => r.MatchStatus == "Matched Drive" || r.MatchStatus == "Matched Semester");
        public int NewStudentsCount => Rows.Count(r => r.MatchStatus == "New Student");
        public int AbsentCount => Rows.Count(r => r.IsAbsent);
        public List<string> DetectedHeaders { get; set; } = new();
        public List<string> HandledNotes { get; set; } = new();
        public List<MockImportRow> Rows { get; set; } = new();
        public string? WarningMessage { get; set; }
    }

    /// <summary>
    /// Parses an uploaded Excel (.xlsx, .xls) or CSV file with forgiving column detection.
    /// Works seamlessly even if 2-3 columns or data cells are missing.
    /// </summary>
    public async Task<MockImportParseResult> ParseEvaluationSheetAsync(Stream fileStream, string fileName, int driveId)
    {
        var result = new MockImportParseResult
        {
            FileName = fileName
        };

        // Fetch existing drive and registered students for matching
        var drive = await _db.MockInterviewDrives
            .Include(d => d.Evaluations)
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(d => d.Id == driveId);

        if (drive == null)
        {
            result.WarningMessage = "Active mock interview drive not found.";
            return result;
        }

        var semesterStudents = await _db.Students
            .Where(s => s.SemesterId == drive.SemesterId)
            .ToListAsync();

        List<List<string>> rawGrid;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext == ".csv" || ext == ".txt")
        {
            rawGrid = ParseCsvToGrid(fileStream);
        }
        else
        {
            // Default to ClosedXML for .xlsx / spreadsheets
            rawGrid = ParseExcelToGrid(fileStream);
        }

        if (rawGrid.Count == 0)
        {
            result.WarningMessage = "The uploaded file is empty or could not be read.";
            return result;
        }

        // Detect header row (scan first 15 rows)
        var (headerRowIdx, mapping) = DetectHeaderRow(rawGrid);
        result.DetectedHeaders = mapping.DetectedColumns;
        result.HandledNotes = mapping.HandledOmissions;

        // Parse data rows starting after header row
        int dataStartRow = headerRowIdx >= 0 ? headerRowIdx + 1 : 0;
        int rowCounter = 1;

        for (int r = dataStartRow; r < rawGrid.Count; r++)
        {
            var row = rawGrid[r];
            if (IsRowEmptyOrSummary(row))
                continue;

            var importRow = new MockImportRow
            {
                RowIndex = rowCounter++
            };

            // Extract Name
            if (mapping.NameColIndex >= 0 && mapping.NameColIndex < row.Count)
                importRow.RawName = row[mapping.NameColIndex]?.Trim() ?? "";

            // Extract Roll
            if (mapping.RollColIndex >= 0 && mapping.RollColIndex < row.Count)
                importRow.RawRoll = CleanRoll(row[mapping.RollColIndex]?.Trim() ?? "");

            // If neither Name nor Roll column mapped, use column 0 or 1 heuristics
            if (string.IsNullOrWhiteSpace(importRow.RawName) && string.IsNullOrWhiteSpace(importRow.RawRoll))
            {
                if (row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
                {
                    var val0 = row[0].Trim();
                    if (Regex.IsMatch(val0, @"^\d{5,}$"))
                        importRow.RawRoll = val0;
                    else if (!IsNumeric(val0))
                        importRow.RawName = val0;
                }
                if (row.Count > 1 && !string.IsNullOrWhiteSpace(row[1]) && string.IsNullOrWhiteSpace(importRow.RawName))
                {
                    importRow.RawName = row[1].Trim();
                }
            }

            // Skip if still completely empty identifier
            if (string.IsNullOrWhiteSpace(importRow.RawName) && string.IsNullOrWhiteSpace(importRow.RawRoll))
                continue;

            // Extract Attendance / Absent
            if (mapping.AbsentColIndex >= 0 && mapping.AbsentColIndex < row.Count)
            {
                var absVal = row[mapping.AbsentColIndex]?.Trim().ToLowerInvariant() ?? "";
                if (absVal is "yes" or "true" or "absent" or "ab" or "a" or "y" or "1")
                    importRow.IsAbsent = true;
            }

            // Extract Scores
            importRow.Confidence = ParseDecimalScore(mapping.ConfColIndex >= 0 && mapping.ConfColIndex < row.Count ? row[mapping.ConfColIndex] : null, out bool confAbsent);
            importRow.Communication = ParseDecimalScore(mapping.CommColIndex >= 0 && mapping.CommColIndex < row.Count ? row[mapping.CommColIndex] : null, out bool commAbsent);
            importRow.Technical = ParseDecimalScore(mapping.TechColIndex >= 0 && mapping.TechColIndex < row.Count ? row[mapping.TechColIndex] : null, out bool techAbsent);
            importRow.Marks = ParseDecimalScore(mapping.MarksColIndex >= 0 && mapping.MarksColIndex < row.Count ? row[mapping.MarksColIndex] : null, out bool marksAbsent);

            // If any cell contains "absent" or "AB", mark absent
            if (confAbsent || commAbsent || techAbsent || marksAbsent)
            {
                importRow.IsAbsent = true;
            }

            // Auto-handle missing marks or factors
            if (importRow.Marks == null && !importRow.IsAbsent)
            {
                var factors = new List<decimal>();
                if (importRow.Confidence.HasValue) factors.Add(importRow.Confidence.Value);
                if (importRow.Communication.HasValue) factors.Add(importRow.Communication.Value);
                if (importRow.Technical.HasValue) factors.Add(importRow.Technical.Value);

                if (factors.Count > 0)
                {
                    // Auto-calculate average of available factors
                    importRow.Marks = Math.Round(factors.Average(), 1);
                }
            }

            // If Marks is present but factors are missing, leave factors as null (completely valid)
            if (importRow.IsAbsent)
            {
                importRow.Confidence = null;
                importRow.Communication = null;
                importRow.Technical = null;
                importRow.Marks = null;
            }

            // Extract Feedback
            if (mapping.FeedbackColIndex >= 0 && mapping.FeedbackColIndex < row.Count)
            {
                importRow.Feedback = row[mapping.FeedbackColIndex]?.Trim();
            }

            if (importRow.IsAbsent && string.IsNullOrWhiteSpace(importRow.Feedback))
            {
                importRow.Feedback = "absent";
            }

            // Match student against drive evaluations or semester roster
            MatchStudent(importRow, drive, semesterStudents);

            result.Rows.Add(importRow);
        }

        result.TotalRowsRead = result.Rows.Count;
        return result;
    }

    /// <summary>
    /// Applies the parsed mock interview data to the drive.
    /// Updates existing evaluations or enrolls/adds missing students cleanly.
    /// </summary>
    public async Task<(int updatedCount, int createdCount, int absentCount)> ApplyImportAsync(
        int driveId, 
        List<MockImportRow> rows, 
        bool autoCreateMissingStudents = true)
    {
        var drive = await _db.MockInterviewDrives
            .Include(d => d.Evaluations)
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(d => d.Id == driveId);

        if (drive == null)
            throw new InvalidOperationException("Mock interview drive not found.");

        var semesterStudents = await _db.Students
            .Where(s => s.SemesterId == drive.SemesterId)
            .ToListAsync();

        int updatedCount = 0;
        int createdCount = 0;
        int absentCount = 0;

        var existingEvalMap = drive.Evaluations
            .Where(e => e.Student != null)
            .ToDictionary(e => e.StudentId, e => e);

        // Pre-compute next student code if new students need to be created
        long nextCodeNum = 1272262350;
        var existingCodes = await _db.Students.Select(s => s.StudentCode).ToListAsync();
        foreach (var c in existingCodes)
        {
            if (long.TryParse(c, out var val) && val > nextCodeNum)
                nextCodeNum = val;
        }

        foreach (var row in rows.Where(r => r.IsSelected))
        {
            Student? targetStudent = null;

            // 1. Try student ID from match
            if (row.MatchedStudentId.HasValue && row.MatchedStudentId.Value > 0)
            {
                targetStudent = semesterStudents.FirstOrDefault(s => s.Id == row.MatchedStudentId.Value)
                    ?? await _db.Students.FindAsync(row.MatchedStudentId.Value);
            }

            // 2. Try match by Roll No
            if (targetStudent == null && !string.IsNullOrWhiteSpace(row.RawRoll))
            {
                targetStudent = semesterStudents.FirstOrDefault(s => CleanRoll(s.StudentCode).Equals(CleanRoll(row.RawRoll), StringComparison.OrdinalIgnoreCase));
            }

            // 3. Try match by Name
            if (targetStudent == null && !string.IsNullOrWhiteSpace(row.RawName))
            {
                var cleanImportName = NormalizeName(row.RawName);
                targetStudent = semesterStudents.FirstOrDefault(s => NormalizeName(s.FullName).Equals(cleanImportName, StringComparison.OrdinalIgnoreCase));
            }

            // 4. If student not found and auto-create enabled, create student in semester
            if (targetStudent == null && autoCreateMissingStudents && (!string.IsNullOrWhiteSpace(row.RawName) || !string.IsNullOrWhiteSpace(row.RawRoll)))
            {
                var newRoll = !string.IsNullOrWhiteSpace(row.RawRoll) ? row.RawRoll.Trim() : (++nextCodeNum).ToString();
                var newName = !string.IsNullOrWhiteSpace(row.RawName) ? row.RawName.Trim() : $"Student {newRoll}";

                targetStudent = new Student
                {
                    StudentCode = newRoll,
                    FullName = newName,
                    SemesterId = drive.SemesterId,
                    EnrollmentStatus = "Active"
                };
                _db.Students.Add(targetStudent);
                await _db.SaveChangesAsync();
                semesterStudents.Add(targetStudent);
                createdCount++;
            }

            if (targetStudent == null)
                continue;

            // Check if evaluation already exists in this drive
            if (existingEvalMap.TryGetValue(targetStudent.Id, out var existingEval))
            {
                // Update existing evaluation
                existingEval.IsAbsent = row.IsAbsent;
                if (row.IsAbsent)
                {
                    existingEval.ConfidenceScore = null;
                    existingEval.CommunicationScore = null;
                    existingEval.TechnicalScore = null;
                    existingEval.MarksOutOf10 = null;
                    existingEval.Status = "Absent";
                    existingEval.Feedback = string.IsNullOrWhiteSpace(row.Feedback) ? "absent" : row.Feedback.Trim();
                    absentCount++;
                }
                else
                {
                    existingEval.ConfidenceScore = row.Confidence;
                    existingEval.CommunicationScore = row.Communication;
                    existingEval.TechnicalScore = row.Technical;
                    existingEval.MarksOutOf10 = row.Marks;
                    existingEval.Status = "Completed";
                    if (!string.IsNullOrWhiteSpace(row.Feedback))
                        existingEval.Feedback = row.Feedback.Trim();
                }
                existingEval.CourseId = drive.CourseId;
                existingEval.SubjectName = drive.SubjectName;
                existingEval.InterviewedAt = DateTime.UtcNow;
                updatedCount++;
            }
            else
            {
                // Create new evaluation for drive
                var newEval = new MockInterviewEvaluation
                {
                    DriveId = drive.Id,
                    StudentId = targetStudent.Id,
                    CourseId = drive.CourseId,
                    SubjectName = drive.SubjectName,
                    StudentGroup = "",
                    IsAbsent = row.IsAbsent,
                    Status = row.IsAbsent ? "Absent" : "Completed",
                    ConfidenceScore = row.IsAbsent ? null : row.Confidence,
                    CommunicationScore = row.IsAbsent ? null : row.Communication,
                    TechnicalScore = row.IsAbsent ? null : row.Technical,
                    MarksOutOf10 = row.IsAbsent ? null : row.Marks,
                    Feedback = row.IsAbsent ? (string.IsNullOrWhiteSpace(row.Feedback) ? "absent" : row.Feedback.Trim()) : row.Feedback?.Trim(),
                    InterviewedAt = DateTime.UtcNow
                };
                _db.MockInterviewEvaluations.Add(newEval);
                existingEvalMap[targetStudent.Id] = newEval;
                updatedCount++;
                if (row.IsAbsent) absentCount++;
            }
        }

        await _db.SaveChangesAsync();
        return (updatedCount, createdCount, absentCount);
    }

    /// <summary>
    /// Generates a ready-to-use sample template (.xlsx or .csv) pre-filled with the active drive's student roster.
    /// </summary>
    public async Task<byte[]> GenerateExcelTemplateAsync(int driveId)
    {
        var drive = await _db.MockInterviewDrives
            .Include(d => d.Evaluations)
                .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(d => d.Id == driveId);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Mock Interview Marks");

        // Header Styling
        ws.Cell(1, 1).Value = "Sr";
        ws.Cell(1, 2).Value = "Student Name";
        ws.Cell(1, 3).Value = "Roll No";
        ws.Cell(1, 4).Value = "Confidence (0-10)";
        ws.Cell(1, 5).Value = "Communication (0-10)";
        ws.Cell(1, 6).Value = "Technical (0-10)";
        ws.Cell(1, 7).Value = "Marks / 10";
        ws.Cell(1, 8).Value = "Evaluator Feedback";
        ws.Cell(1, 9).Value = "Absent (Yes/No)";

        var headerRange = ws.Range("A1:I1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(37, 99, 235); // Blue
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 2;
        if (drive != null && drive.Evaluations.Any())
        {
            var sortedEvals = drive.Evaluations
                .OrderBy(e => e.Student?.StudentCode ?? "")
                .ThenBy(e => e.Student?.FullName ?? "");

            foreach (var eval in sortedEvals)
            {
                ws.Cell(row, 1).Value = row - 1;
                ws.Cell(row, 2).Value = eval.Student?.FullName ?? "";
                ws.Cell(row, 3).Value = eval.Student?.StudentCode ?? "";
                if (eval.ConfidenceScore.HasValue) ws.Cell(row, 4).Value = eval.ConfidenceScore.Value;
                if (eval.CommunicationScore.HasValue) ws.Cell(row, 5).Value = eval.CommunicationScore.Value;
                if (eval.TechnicalScore.HasValue) ws.Cell(row, 6).Value = eval.TechnicalScore.Value;
                if (eval.MarksOutOf10.HasValue) ws.Cell(row, 7).Value = eval.MarksOutOf10.Value;
                ws.Cell(row, 8).Value = eval.Feedback ?? "";
                ws.Cell(row, 9).Value = eval.IsAbsent ? "Yes" : "No";
                row++;
            }
        }
        else
        {
            // Sample placeholder rows
            ws.Cell(2, 1).Value = 1;
            ws.Cell(2, 2).Value = "Sample Student";
            ws.Cell(2, 3).Value = "1272262301";
            ws.Cell(2, 4).Value = 8.5;
            ws.Cell(2, 5).Value = 8.0;
            ws.Cell(2, 6).Value = 9.0;
            ws.Cell(2, 7).Value = 8.5;
            ws.Cell(2, 8).Value = "Strong SQL query fundamentals and good confidence.";
            ws.Cell(2, 9).Value = "No";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // --- Private Helper Methods ---

    private static (int headerRowIdx, MockImportColumnMapping mapping) DetectHeaderRow(List<List<string>> grid)
    {
        var mapping = new MockImportColumnMapping();
        int bestRowIdx = -1;
        int maxMatchedScore = 0;

        int scanLimit = Math.Min(grid.Count, 15);
        for (int r = 0; r < scanLimit; r++)
        {
            var row = grid[r];
            var tempMapping = new MockImportColumnMapping();
            int score = 0;

            for (int c = 0; c < row.Count; c++)
            {
                var rawHeader = row[c]?.Trim() ?? "";
                var norm = Normalize(rawHeader);
                if (string.IsNullOrEmpty(norm)) continue;

                if (IsNameHeader(norm) && tempMapping.NameColIndex == -1)
                {
                    tempMapping.NameColIndex = c;
                    score += 5;
                }
                else if (IsRollHeader(norm) && tempMapping.RollColIndex == -1)
                {
                    tempMapping.RollColIndex = c;
                    score += 5;
                }
                else if (IsConfHeader(norm) && tempMapping.ConfColIndex == -1)
                {
                    tempMapping.ConfColIndex = c;
                    score += 3;
                }
                else if (IsCommHeader(norm) && tempMapping.CommColIndex == -1)
                {
                    tempMapping.CommColIndex = c;
                    score += 3;
                }
                else if (IsTechHeader(norm) && tempMapping.TechColIndex == -1)
                {
                    tempMapping.TechColIndex = c;
                    score += 3;
                }
                else if (IsFeedbackHeader(norm) && tempMapping.FeedbackColIndex == -1)
                {
                    tempMapping.FeedbackColIndex = c;
                    score += 4;
                }
                else if (IsMarksHeader(norm) && tempMapping.MarksColIndex == -1)
                {
                    tempMapping.MarksColIndex = c;
                    score += 4;
                }
                else if (IsAbsentHeader(norm) && tempMapping.AbsentColIndex == -1)
                {
                    tempMapping.AbsentColIndex = c;
                    score += 2;
                }
            }

            if (score > maxMatchedScore)
            {
                maxMatchedScore = score;
                bestRowIdx = r;
                mapping = tempMapping;
            }
        }

        // If no recognized header found, deduce from first row or use standard indices
        if (bestRowIdx == -1)
        {
            bestRowIdx = -1; // Data starts from row 0
            mapping.NameColIndex = 1;
            mapping.RollColIndex = 0;
            mapping.MarksColIndex = 2;
            mapping.HandledOmissions.Add("No header row found: auto-assigned Col 1 = Roll, Col 2 = Name, Col 3 = Marks.");
        }
        else
        {
            // Record detected columns
            var headerRow = grid[bestRowIdx];
            if (mapping.NameColIndex >= 0 && mapping.NameColIndex < headerRow.Count)
                mapping.DetectedColumns.Add($"Student Name (Col {mapping.NameColIndex + 1}: '{headerRow[mapping.NameColIndex]}')");
            if (mapping.RollColIndex >= 0 && mapping.RollColIndex < headerRow.Count)
                mapping.DetectedColumns.Add($"Roll No (Col {mapping.RollColIndex + 1}: '{headerRow[mapping.RollColIndex]}')");
            if (mapping.MarksColIndex >= 0 && mapping.MarksColIndex < headerRow.Count)
                mapping.DetectedColumns.Add($"Marks / 10 (Col {mapping.MarksColIndex + 1}: '{headerRow[mapping.MarksColIndex]}')");
            if (mapping.ConfColIndex >= 0 && mapping.ConfColIndex < headerRow.Count)
                mapping.DetectedColumns.Add($"Confidence (Col {mapping.ConfColIndex + 1}: '{headerRow[mapping.ConfColIndex]}')");
            if (mapping.CommColIndex >= 0 && mapping.CommColIndex < headerRow.Count)
                mapping.DetectedColumns.Add($"Communication (Col {mapping.CommColIndex + 1}: '{headerRow[mapping.CommColIndex]}')");
            if (mapping.TechColIndex >= 0 && mapping.TechColIndex < headerRow.Count)
                mapping.DetectedColumns.Add($"Technical (Col {mapping.TechColIndex + 1}: '{headerRow[mapping.TechColIndex]}')");
            if (mapping.FeedbackColIndex >= 0 && mapping.FeedbackColIndex < headerRow.Count)
                mapping.DetectedColumns.Add($"Feedback (Col {mapping.FeedbackColIndex + 1}: '{headerRow[mapping.FeedbackColIndex]}')");
            if (mapping.AbsentColIndex >= 0 && mapping.AbsentColIndex < headerRow.Count)
                mapping.DetectedColumns.Add($"Absent (Col {mapping.AbsentColIndex + 1}: '{headerRow[mapping.AbsentColIndex]}')");

            // Record handled omissions
            if (mapping.MarksColIndex == -1 && (mapping.ConfColIndex != -1 || mapping.CommColIndex != -1 || mapping.TechColIndex != -1))
                mapping.HandledOmissions.Add("Marks column omitted: Automatically calculated from 3 factor scores (average out of 10).");
            if (mapping.ConfColIndex == -1 && mapping.CommColIndex == -1 && mapping.TechColIndex == -1 && mapping.MarksColIndex != -1)
                mapping.HandledOmissions.Add("Factor columns omitted: Marks imported directly, factors kept flexible.");
            if (mapping.RollColIndex == -1 && mapping.NameColIndex != -1)
                mapping.HandledOmissions.Add("Roll No column omitted: Matching students automatically by Full Name.");
            if (mapping.NameColIndex == -1 && mapping.RollColIndex != -1)
                mapping.HandledOmissions.Add("Name column omitted: Matching students automatically by Roll No / Student Code.");
            if (mapping.FeedbackColIndex == -1)
                mapping.HandledOmissions.Add("Feedback column omitted: Importing marks without overwriting existing notes.");
        }

        return (bestRowIdx, mapping);
    }

    private static void MatchStudent(MockImportRow row, MockInterviewDrive drive, List<Student> semesterStudents)
    {
        // Check drive's evaluations first
        var cleanRoll = CleanRoll(row.RawRoll);
        var cleanName = NormalizeName(row.RawName);

        // 1. Try match by Roll in Drive
        if (!string.IsNullOrWhiteSpace(cleanRoll))
        {
            var matchDriveEval = drive.Evaluations.FirstOrDefault(e => e.Student != null && CleanRoll(e.Student.StudentCode).Equals(cleanRoll, StringComparison.OrdinalIgnoreCase));
            if (matchDriveEval != null && matchDriveEval.Student != null)
            {
                row.MatchedStudentId = matchDriveEval.StudentId;
                row.MatchedStudentName = matchDriveEval.Student.FullName;
                row.MatchedStudentRoll = matchDriveEval.Student.StudentCode;
                row.MatchStatus = "Matched Drive";
                return;
            }
        }

        // 2. Try match by Name in Drive
        if (!string.IsNullOrWhiteSpace(cleanName))
        {
            var matchDriveEval = drive.Evaluations.FirstOrDefault(e => e.Student != null && NormalizeName(e.Student.FullName).Equals(cleanName, StringComparison.OrdinalIgnoreCase));
            if (matchDriveEval != null && matchDriveEval.Student != null)
            {
                row.MatchedStudentId = matchDriveEval.StudentId;
                row.MatchedStudentName = matchDriveEval.Student.FullName;
                row.MatchedStudentRoll = matchDriveEval.Student.StudentCode;
                row.MatchStatus = "Matched Drive";
                return;
            }
        }

        // 3. Try match in Semester Roster
        if (!string.IsNullOrWhiteSpace(cleanRoll))
        {
            var semStudent = semesterStudents.FirstOrDefault(s => CleanRoll(s.StudentCode).Equals(cleanRoll, StringComparison.OrdinalIgnoreCase));
            if (semStudent != null)
            {
                row.MatchedStudentId = semStudent.Id;
                row.MatchedStudentName = semStudent.FullName;
                row.MatchedStudentRoll = semStudent.StudentCode;
                row.MatchStatus = "Matched Semester";
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(cleanName))
        {
            var semStudent = semesterStudents.FirstOrDefault(s => NormalizeName(s.FullName).Equals(cleanName, StringComparison.OrdinalIgnoreCase));
            if (semStudent != null)
            {
                row.MatchedStudentId = semStudent.Id;
                row.MatchedStudentName = semStudent.FullName;
                row.MatchedStudentRoll = semStudent.StudentCode;
                row.MatchStatus = "Matched Semester";
                return;
            }
        }

        // 4. New student to auto-enroll
        row.MatchStatus = "New Student";
        row.MatchedStudentName = row.RawName;
        row.MatchedStudentRoll = row.RawRoll;
    }

    private static List<List<string>> ParseExcelToGrid(Stream stream)
    {
        var grid = new List<List<string>>();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.FirstOrDefault();
        if (ws == null) return grid;

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int r = 1; r <= lastRow; r++)
        {
            var rowList = new List<string>();
            var row = ws.Row(r);
            for (int c = 1; c <= lastCol; c++)
            {
                var cell = row.Cell(c);
                var text = cell.GetString()?.Trim() ?? "";
                rowList.Add(text);
            }
            grid.Add(rowList);
        }

        return grid;
    }

    private static List<List<string>> ParseCsvToGrid(Stream stream)
    {
        var grid = new List<List<string>>();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = ParseCsvLine(line);
            grid.Add(parts);
        }

        return grid;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if ((c == ',' || c == '\t') && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());
        return result;
    }

    private static decimal? ParseDecimalScore(string? raw, out bool isAbsent)
    {
        isAbsent = false;
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var clean = raw.Trim().ToLowerInvariant();
        if (clean is "ab" or "absent" or "a" or "abs")
        {
            isAbsent = true;
            return null;
        }

        if (clean is "-" or "na" or "n/a" or "nil")
            return null;

        // Strip "/10" if present: e.g. "8.5/10" -> "8.5"
        var slashIdx = clean.IndexOf('/');
        if (slashIdx > 0)
            clean = clean.Substring(0, slashIdx).Trim();

        // Handle commas as decimal separator: "8,5" -> "8.5"
        clean = clean.Replace(',', '.');

        // Extract numeric part using regex
        var match = Regex.Match(clean, @"\d+(\.\d+)?");
        if (match.Success && decimal.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
        {
            return Math.Clamp(num, 0, 10);
        }

        return null;
    }

    private static bool IsRowEmptyOrSummary(List<string> row)
    {
        if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace))
            return true;

        var firstNonEmpty = row.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))?.Trim().ToLowerInvariant() ?? "";
        if (firstNonEmpty.StartsWith("total") ||
            firstNonEmpty.StartsWith("average") ||
            firstNonEmpty.StartsWith("avg") ||
            firstNonEmpty.StartsWith("grand total") ||
            firstNonEmpty.StartsWith("signed by") ||
            firstNonEmpty.StartsWith("remarks:") ||
            firstNonEmpty.StartsWith("note:") ||
            firstNonEmpty.StartsWith("mit world peace") ||
            firstNonEmpty.StartsWith("#"))
        {
            return true;
        }

        return false;
    }

    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        return new string(input.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var clean = Regex.Replace(name.Trim(), @"\s+", " ").ToLowerInvariant();
        // Remove salutations: mr., ms., mrs.
        clean = Regex.Replace(clean, @"^(mr\.|ms\.|mrs\.|dr\.)\s*", "");
        return clean;
    }

    private static string CleanRoll(string? roll)
    {
        if (string.IsNullOrWhiteSpace(roll)) return "";
        return Regex.Replace(roll.Trim(), @"[\s\-\.]", "");
    }

    private static bool IsNumeric(string str) => Regex.IsMatch(str.Trim(), @"^\d+(\.\d+)?$");

    private static bool IsNameHeader(string norm) =>
        norm.Contains("studentname") || norm.Contains("candidatename") || norm.Equals("name") ||
        norm.Equals("student") || norm.Equals("candidate") || norm.Contains("fullname");

    private static bool IsRollHeader(string norm) =>
        norm.Contains("roll") || norm.Contains("prn") || norm.Contains("studentcode") ||
        norm.Contains("registration") || norm.Contains("regno") || norm.Equals("code") ||
        norm.Equals("prnno") || norm.Equals("rollno") || norm.Equals("rollnumber");

    private static bool IsConfHeader(string norm) =>
        norm.Contains("confidence") || norm.StartsWith("conf") || norm.Contains("factor1");

    private static bool IsCommHeader(string norm) =>
        norm.Contains("communication") || norm.StartsWith("comm") || norm.Contains("factor2");

    private static bool IsTechHeader(string norm) =>
        norm.Contains("technical") || norm.StartsWith("tech") || norm.Contains("theory") || norm.Contains("factor3");

    private static bool IsMarksHeader(string norm) =>
        !norm.Contains("remark") &&
        (norm.Contains("marks") || norm.Equals("score") || norm.Contains("total") ||
        norm.Contains("finalscore") || norm.Equals("overall") || norm.Contains("grade"));

    private static bool IsFeedbackHeader(string norm) =>
        norm.Contains("feedback") || norm.Contains("remarks") || norm.Contains("notes") ||
        norm.Contains("comment") || norm.Contains("suggestion") || norm.Contains("review");

    private static bool IsAbsentHeader(string norm) =>
        norm.Contains("absent") || norm.Equals("status") || norm.Contains("attendance") || norm.Equals("present");
}
