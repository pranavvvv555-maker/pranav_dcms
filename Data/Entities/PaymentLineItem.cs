namespace DCMSApp.Data.Entities;

public class PaymentLineItem
{
    public int Id { get; set; }
    public int PaymentPeriodId { get; set; }
    public int FacultyId { get; set; }
    public int LectureCount { get; set; }
    public int PracticalCount { get; set; }
    public decimal LectureRate { get; set; }
    public decimal PracticalRate { get; set; }
    public decimal TotalHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetPayable { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public PaymentPeriod PaymentPeriod { get; set; } = null!;
    public Faculty Faculty { get; set; } = null!;
}
