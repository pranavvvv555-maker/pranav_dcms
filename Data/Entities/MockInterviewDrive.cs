namespace DCMSApp.Data.Entities;

public class MockInterviewDrive
{
    public int Id { get; set; }
    public int SemesterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime DriveDate { get; set; } = DateTime.Today;
    public string Status { get; set; } = "Completed"; // Scheduled, In Progress, Completed
    public string? PanelNotes { get; set; }

    // Navigation
    public Semester Semester { get; set; } = null!;
    public ICollection<MockInterviewEvaluation> Evaluations { get; set; } = new List<MockInterviewEvaluation>();
}
