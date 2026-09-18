namespace DCMSApp.Data.Entities;

public class Faculty
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Qualification { get; set; }
    public string? ExperienceYears { get; set; }
    public string? Domain { get; set; }
    public string Role { get; set; } = "Teaching"; // Teaching or Lab Assistant
    public string? OfficialEmail { get; set; }
    public string? PersonalEmail { get; set; }
    public string? Phone { get; set; }
    public string Organization { get; set; } = "NIRVAA"; // NIRVAA or MIT-WPU
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<FacultyRate> Rates { get; set; } = new List<FacultyRate>();
    public ICollection<CourseFaculty> CourseFaculties { get; set; } = new List<CourseFaculty>();
    public ICollection<TimetableSlot> TimetableSlots { get; set; } = new List<TimetableSlot>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<PaymentLineItem> PaymentLineItems { get; set; } = new List<PaymentLineItem>();
}
