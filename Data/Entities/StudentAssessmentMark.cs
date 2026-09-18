namespace DCMSApp.Data.Entities;

public class StudentAssessmentMark
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public int StudentId { get; set; }
    public decimal? MarksObtained { get; set; }
    public bool IsAbsent { get; set; }
    public string? Remarks { get; set; }
    public DateTime? GradedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Assessment Assessment { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
