namespace DCMSApp.Data.Entities;

/// <summary>
/// One student's attendance for one conducted class.
/// A record is stored for each active student when attendance is submitted,
/// so a saved register can distinguish "absent" from "not yet marked".
/// </summary>
public class SessionAttendance
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int StudentId { get; set; }
    public bool IsPresent { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Session Session { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
