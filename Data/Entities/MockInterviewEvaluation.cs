namespace DCMSApp.Data.Entities;

public class MockInterviewEvaluation
{
    public int Id { get; set; }
    public int DriveId { get; set; }
    public int StudentId { get; set; }
    public int? InterviewerFacultyId { get; set; }

    // Student Group (e.g. "Group A", "Group B")
    public string StudentGroup { get; set; } = "Group A";

    // 3 Factors out of 10
    public decimal? ConfidenceScore { get; set; }
    public decimal? CommunicationScore { get; set; }
    public decimal? TechnicalScore { get; set; }

    // Overall Marks out of 10
    public decimal? MarksOutOf10 { get; set; }

    // Attendance & Feedback
    public bool IsAbsent { get; set; }
    public string? Feedback { get; set; }

    public string Status { get; set; } = "Completed"; // Scheduled, Completed, Absent
    public DateTime? InterviewedAt { get; set; } = DateTime.UtcNow;

    // Subject / Course linkage
    public int? CourseId { get; set; }
    public string? SubjectName { get; set; }

    // Navigation
    public MockInterviewDrive Drive { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public Faculty? Interviewer { get; set; }
    public Course? Course { get; set; }
}
