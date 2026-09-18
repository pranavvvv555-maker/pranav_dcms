namespace DCMSApp.Data.Entities;

public class PaymentAdjustment
{
    public int Id { get; set; }
    public int PaymentPeriodId { get; set; }
    public int FacultyId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Bonus"; // Bonus, Deduction, Advance
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PaymentPeriod PaymentPeriod { get; set; } = null!;
    public Faculty Faculty { get; set; } = null!;
}
