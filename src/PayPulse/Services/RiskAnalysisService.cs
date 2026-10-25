using System;
using System.Collections.Generic;
using System.Linq;
using PayPulse.Models;

namespace PayPulse.Services;

public class RiskAnalysisService
{
    private readonly InvoiceCalculationService _calcService;

    public RiskAnalysisService(InvoiceCalculationService calcService)
    {
        _calcService = calcService;
    }

    public RiskAssessmentResult AssessInvoiceRisk(Invoice invoice, List<Invoice> allCustomerInvoices, DateTime? referenceDate = null)
    {
        DateTime today = referenceDate?.Date ?? DateTime.Today;
        decimal balance = _calcService.CalculateOutstandingBalance(invoice);
        int daysOverdue = invoice.DueDate.Date < today ? (today - invoice.DueDate.Date).Days : 0;

        int score = 0;
        var factors = new List<string>();

        // If draft or fully paid, risk is 0 (Low)
        if (invoice.Status == InvoiceStatus.Draft || balance <= 0)
        {
            return new RiskAssessmentResult
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CustomerName = invoice.Customer?.Name ?? "Unknown",
                OutstandingAmount = balance,
                DaysOverdue = 0,
                RiskLevel = RiskLevel.Low,
                RiskScore = 0,
                Factors = new List<string> { invoice.Status == InvoiceStatus.Draft ? "Invoice is in Draft status." : "Invoice is fully settled." },
                RecommendedAction = "No action required."
            };
        }

        // 1. Days overdue analysis
        if (daysOverdue > 60)
        {
            score += 50;
            factors.Add($"Severely overdue by {daysOverdue} days (> 60 days).");
        }
        else if (daysOverdue > 30)
        {
            score += 35;
            factors.Add($"Overdue by {daysOverdue} days (> 30 days).");
        }
        else if (daysOverdue > 7)
        {
            score += 20;
            factors.Add($"Moderately overdue by {daysOverdue} days (> 1 week).");
        }
        else if (daysOverdue > 0)
        {
            score += 10;
            factors.Add($"Recently overdue by {daysOverdue} days.");
        }
        else
        {
            int daysUntilDue = (invoice.DueDate.Date - today).Days;
            if (daysUntilDue <= 3)
            {
                score += 5;
                factors.Add($"Due very soon ({daysUntilDue} days remaining).");
            }
            else
            {
                factors.Add("Within normal payment term; not yet due.");
            }
        }

        // 2. Outstanding Balance Magnitude
        if (balance >= 100000m)
        {
            score += 25;
            factors.Add($"High monetary exposure (Outstanding: ₹{balance:N2}).");
        }
        else if (balance >= 50000m)
        {
            score += 15;
            factors.Add($"Moderate monetary exposure (Outstanding: ₹{balance:N2}).");
        }

        // 3. Customer Historical Behavior
        var pastInvoices = allCustomerInvoices
            .Where(i => i.Id != invoice.Id && i.Status != InvoiceStatus.Draft)
            .ToList();

        if (pastInvoices.Any())
        {
            int overduePast = pastInvoices.Count(i => _calcService.DetermineStatus(i, today) == InvoiceStatus.Overdue);
            double overdueRatio = (double)overduePast / pastInvoices.Count;

            if (overdueRatio >= 0.5)
            {
                score += 25;
                factors.Add($"Client has high delinquency history ({overduePast} of {pastInvoices.Count} previous invoices overdue).");
            }
            else if (overdueRatio > 0)
            {
                score += 10;
                factors.Add($"Client has occasional overdue records ({overduePast} of {pastInvoices.Count}).");
            }
            else
            {
                score -= 10; // Reward good payment history
                factors.Add("Client has clean past settlement track record.");
            }
        }
        else
        {
            factors.Add("Limited customer payment history on file (New Client).");
        }

        // Normalize score 0 - 100
        score = Math.Clamp(score, 0, 100);

        RiskLevel level = score switch
        {
            >= 60 => RiskLevel.High,
            >= 30 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };

        string action = level switch
        {
            RiskLevel.High => "URGENT: Initiate executive call, place account on credit hold, and dispatch formal demand notice.",
            RiskLevel.Medium => "PRIORITY: Issue phone follow-up and send WhatsApp/Email payment reminder.",
            _ => "STANDARD: Send standard friendly invoice reminder near due date."
        };

        return new RiskAssessmentResult
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerName = invoice.Customer?.Name ?? "Unknown",
            OutstandingAmount = balance,
            DaysOverdue = daysOverdue,
            RiskLevel = level,
            RiskScore = score,
            Factors = factors,
            RecommendedAction = action
        };
    }
}
