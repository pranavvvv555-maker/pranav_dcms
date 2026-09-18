namespace DCMSApp.Data.Entities;

public enum SessionStatus
{
    Scheduled,
    Conducted,
    Cancelled,
    Substituted
}

public class Session
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int? TimetableSlotId { get; set; }
    public int CourseId { get; set; }
    public int FacultyId { get; set; }
    public TimeSpan ActualStartTime { get; set; }
    public TimeSpan ActualEndTime { get; set; }
    public decimal DurationHours { get; set; }
    public string SessionType { get; set; } = "Lecture";
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public int? SubstituteFacultyId { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
    public string? LoggedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public TimetableSlot? TimetableSlot { get; set; }
    public Course Course { get; set; } = null!;
    public Faculty Faculty { get; set; } = null!;
    public Faculty? SubstituteFaculty { get; set; }
    public ICollection<PaymentLineItem> PaymentLineItems { get; set; } = new List<PaymentLineItem>();
    public ICollection<SessionAttendance> AttendanceRecords { get; set; } = new List<SessionAttendance>();
}
