namespace DCMSApp.Data.Entities;

public class AcademicHoliday
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRemovedDay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
