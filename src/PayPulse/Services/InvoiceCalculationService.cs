using System;
using System.Linq;
using PayPulse.Models;

namespace PayPulse.Services;

public class InvoiceCalculationService
{
    public (decimal Subtotal, decimal TaxAmount, decimal TotalAmount) CalculateTotals(
        decimal subtotal, decimal discount, decimal taxRate)
    {
        subtotal = Math.Max(0, subtotal);
        discount = Math.Max(0, discount);
        taxRate = Math.Max(0, taxRate);

        decimal taxable = Math.Max(0, subtotal - discount);
        decimal taxAmount = Math.Round((taxable * taxRate) / 100m, 2, MidpointRounding.AwayFromZero);
        decimal totalAmount = taxable + taxAmount;

        return (subtotal, taxAmount, totalAmount);
    }

    public decimal CalculatePaidAmount(Invoice invoice)
    {
        if (invoice.Payments == null || !invoice.Payments.Any())
        {
            return 0m;
        }
        return invoice.Payments.Sum(p => p.Amount);
    }

    public decimal CalculateOutstandingBalance(Invoice invoice)
    {
        decimal paid = CalculatePaidAmount(invoice);
        decimal balance = invoice.TotalAmount - paid;
        return balance < 0 ? 0m : balance;
    }

    public InvoiceStatus DetermineStatus(Invoice invoice, DateTime? referenceDate = null)
    {
        if (invoice.Status == InvoiceStatus.Draft)
        {
            return InvoiceStatus.Draft;
        }

        DateTime today = referenceDate?.Date ?? DateTime.Today;
        decimal paid = CalculatePaidAmount(invoice);
        decimal balance = invoice.TotalAmount - paid;

        if (balance <= 0)
        {
            return InvoiceStatus.Paid;
        }

        if (invoice.DueDate.Date < today)
        {
            return InvoiceStatus.Overdue;
        }

        if (paid > 0)
        {
            return InvoiceStatus.PartiallyPaid;
        }

        return InvoiceStatus.Unpaid;
    }
}
