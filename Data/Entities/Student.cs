namespace DCMSApp.Data.Entities;

public class Student
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string EnrollmentStatus { get; set; } = "Active";
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string? Gender { get; set; }

    // Navigation
    public Semester Semester { get; set; } = null!;
    public ICollection<SessionAttendance> AttendanceRecords { get; set; } = new List<SessionAttendance>();
}
