namespace DCMSApp.Data.Entities;

public class Course
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? CourseType { get; set; }
    public int Credits { get; set; }
    public int LectureHours { get; set; }
    public int TutorialHours { get; set; }
    public int PracticalHours { get; set; }
    public int ProjectHours { get; set; }
    public string? AssessmentScheme { get; set; }
    public int SemesterId { get; set; }

    // Navigation
    public Semester Semester { get; set; } = null!;
    public ICollection<CourseFaculty> CourseFaculties { get; set; } = new List<CourseFaculty>();
    public ICollection<TimetableSlot> TimetableSlots { get; set; } = new List<TimetableSlot>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
