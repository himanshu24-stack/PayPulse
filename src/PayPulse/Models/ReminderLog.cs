using System;

namespace PayPulse.Models;

public class ReminderLog
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public DateTime ReminderDate { get; set; } = DateTime.UtcNow;
    public string ReminderType { get; set; } = "Email Follow-up";
    public string? Notes { get; set; }
    public string Status { get; set; } = "Contacted";
}
