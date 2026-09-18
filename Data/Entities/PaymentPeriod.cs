namespace DCMSApp.Data.Entities;

public enum PaymentPeriodStatus
{
    Open,
    Calculated,
    Approved,
    Paid
}

public class PaymentPeriod
{
    public int Id { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PaymentPeriodStatus Status { get; set; } = PaymentPeriodStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<PaymentLineItem> LineItems { get; set; } = new List<PaymentLineItem>();
    public ICollection<PaymentAdjustment> Adjustments { get; set; } = new List<PaymentAdjustment>();
}
