using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PayPulse.Data;
using PayPulse.Models;
using PayPulse.Services;
using Xunit;

namespace PayPulse.Tests;

public class BusinessLogicTests
{
    private readonly InvoiceCalculationService _calcService = new();
    private readonly RiskAnalysisService _riskService;

    public BusinessLogicTests()
    {
        _riskService = new RiskAnalysisService(_calcService);
    }

    [Fact]
    public void CalculateTotals_WithTaxAndDiscount_CalculatesAccurately()
    {
        // Arrange: Subtotal ₹100,000, Discount ₹10,000, 18% GST
        decimal subtotal = 100000m;
        decimal discount = 10000m;
        decimal taxRate = 18m;

        // Act
        var (sub, tax, total) = _calcService.CalculateTotals(subtotal, discount, taxRate);

        // Assert:
        // Taxable = 90,000
        // Tax = 90,000 * 0.18 = 16,200
        // Total = 90,000 + 16,200 = 106,200
        Assert.Equal(100000m, sub);
        Assert.Equal(16200m, tax);
        Assert.Equal(106200m, total);
    }

    [Fact]
    public void DetermineStatus_WhenFullyPaid_ReturnsPaid()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 25000m,
            DueDate = DateTime.Today.AddDays(-10),
            Status = InvoiceStatus.Unpaid,
            Payments = new List<Payment>
            {
                new Payment { Amount = 25000m, PaymentDate = DateTime.Today.AddDays(-12) }
            }
        };

        // Act
        var status = _calcService.DetermineStatus(invoice, DateTime.Today);

        // Assert
        Assert.Equal(InvoiceStatus.Paid, status);
    }

    [Fact]
    public void DetermineStatus_WhenPastDueDateAndUnpaid_ReturnsOverdue()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 50000m,
            DueDate = DateTime.Today.AddDays(-5),
            Status = InvoiceStatus.Unpaid,
            Payments = new List<Payment>()
        };

        // Act
        var status = _calcService.DetermineStatus(invoice, DateTime.Today);

        // Assert
        Assert.Equal(InvoiceStatus.Overdue, status);
    }

    [Fact]
    public void DetermineStatus_WhenPartiallyPaidAndPastDueDate_ReturnsOverdue()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 50000m,
            DueDate = DateTime.Today.AddDays(-2),
            Status = InvoiceStatus.PartiallyPaid,
            Payments = new List<Payment>
            {
                new Payment { Amount = 20000m, PaymentDate = DateTime.Today.AddDays(-10) }
            }
        };

        // Act
        var status = _calcService.DetermineStatus(invoice, DateTime.Today);

        // Assert
        Assert.Equal(InvoiceStatus.Overdue, status);
    }

    [Fact]
    public void DetermineStatus_WhenPartiallyPaidAndFutureDue_ReturnsPartiallyPaid()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 50000m,
            DueDate = DateTime.Today.AddDays(15),
            Status = InvoiceStatus.PartiallyPaid,
            Payments = new List<Payment>
            {
                new Payment { Amount = 15000m, PaymentDate = DateTime.Today.AddDays(-2) }
            }
        };

        // Act
        var status = _calcService.DetermineStatus(invoice, DateTime.Today);

        // Assert
        Assert.Equal(InvoiceStatus.PartiallyPaid, status);
    }

    [Fact]
    public void DetermineStatus_DraftInvoice_RemainsDraft()
    {
        // Arrange
        var invoice = new Invoice
        {
            TotalAmount = 10000m,
            DueDate = DateTime.Today.AddDays(-10),
            Status = InvoiceStatus.Draft
        };

        // Act
        var status = _calcService.DetermineStatus(invoice, DateTime.Today);

        // Assert
        Assert.Equal(InvoiceStatus.Draft, status);
    }

    [Fact]
    public void RiskAnalysis_SeverelyOverdueWithHighExposure_ClassifiedAsHighRisk()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = 1,
            InvoiceNumber = "INV-HIGH-RISK",
            CustomerId = 1,
            DueDate = DateTime.Today.AddDays(-45), // > 30 days
            TotalAmount = 120000m,               // High exposure
            Status = InvoiceStatus.Overdue,
            Payments = new List<Payment>()
        };

        var allCustomerInvoices = new List<Invoice> { invoice };

        // Act
        var assessment = _riskService.AssessInvoiceRisk(invoice, allCustomerInvoices, DateTime.Today);

        // Assert
        Assert.Equal(RiskLevel.High, assessment.RiskLevel);
        Assert.True(assessment.RiskScore >= 60);
        Assert.Contains(assessment.Factors, f => f.ToLower().Contains("overdue"));
    }

    [Fact]
    public async System.Threading.Tasks.Task DatabaseSeeder_SeedsCorrectEntities()
    {
        // Arrange InMemory DB
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        // Act
        await DatabaseSeeder.SeedSampleDataAsync(context);

        // Assert
        Assert.True(await context.Customers.AnyAsync());
        Assert.True(await context.Invoices.AnyAsync());
        Assert.True(await context.Payments.AnyAsync());
        Assert.True(await context.ReminderLogs.AnyAsync());
    }
}
