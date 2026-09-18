namespace DCMSApp.Data.Entities;

public class CourseFaculty
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int FacultyId { get; set; }
    public string Role { get; set; } = "Lead"; // Lead, Co-faculty, Lab

    // Navigation
    public Course Course { get; set; } = null!;
    public Faculty Faculty { get; set; } = null!;
}
