namespace DCMSApp.Data.Entities;

public class MockInterviewDrive
{
    public int Id { get; set; }
    public int SemesterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DriveDate { get; set; } = DateTime.Today;
    public string Status { get; set; } = "Completed"; // Scheduled, In Progress, Completed
    public string? PanelNotes { get; set; }

    public int? CourseId { get; set; }
    public string? SubjectName { get; set; }

    // Navigation
    public Semester Semester { get; set; } = null!;
    public Course? Course { get; set; }
    public ICollection<MockInterviewEvaluation> Evaluations { get; set; } = new List<MockInterviewEvaluation>();
}
