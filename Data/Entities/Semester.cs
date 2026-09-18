namespace DCMSApp.Data.Entities;

public class Semester
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    // Navigation
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<TimetableSlot> TimetableSlots { get; set; } = new List<TimetableSlot>();
}
