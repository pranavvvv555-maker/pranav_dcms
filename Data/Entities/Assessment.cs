namespace DCMSApp.Data.Entities;

public class Assessment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "Unit Test"; // Unit Test, Lab Practical, Assignment, Mid-Term, Project Evaluation
    public decimal MaxMarks { get; set; } = 25;
    public decimal WeightagePercent { get; set; } = 20;
    public DateTime AssessmentDate { get; set; } = DateTime.Today;
    public int SemesterId { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Evaluated"; // Scheduled, Evaluated, Published

    // Navigation
    public Course Course { get; set; } = null!;
    public Semester Semester { get; set; } = null!;
    public ICollection<StudentAssessmentMark> Marks { get; set; } = new List<StudentAssessmentMark>();
}
