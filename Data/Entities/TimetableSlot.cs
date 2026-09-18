namespace DCMSApp.Data.Entities;

public class TimetableSlot
{
    public int Id { get; set; }
    public int SemesterId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int SlotNumber { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int? CourseId { get; set; }
    public int? FacultyId { get; set; }
    public string SlotType { get; set; } = "Lecture";
    public string? RoomNo { get; set; }

    // Navigation
    public Semester Semester { get; set; } = null!;
    public Course? Course { get; set; }
    public Faculty? Faculty { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
