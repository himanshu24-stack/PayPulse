using System;
using System.Collections.Generic;

namespace PayPulse.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

    public string? Description { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; } // e.g. 18.0 for 18% GST
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<ReminderLog> ReminderLogs { get; set; } = new List<ReminderLog>();
}
