namespace DCMSApp.Data.Entities;

public class FacultyRate
{
    public int Id { get; set; }
    public int FacultyId { get; set; }
    // Retained for compatibility with existing records. New calculations use the two class rates below.
    public decimal HourlyRateINR { get; set; }
    public decimal LectureRateINR { get; set; }
    public decimal PracticalRateINR { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Faculty Faculty { get; set; } = null!;
}
