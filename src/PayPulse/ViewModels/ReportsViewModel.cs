using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PayPulse.Data;
using PayPulse.Models;
using PayPulse.Services;

namespace PayPulse.ViewModels;

public class ReportRow
{
    public string Col1 { get; set; } = string.Empty;
    public string Col2 { get; set; } = string.Empty;
    public string Col3 { get; set; } = string.Empty;
    public string Col4 { get; set; } = string.Empty;
    public string Col5 { get; set; } = string.Empty;
    public string Col6 { get; set; } = string.Empty;
}

public partial class ReportsViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly InvoiceCalculationService _calcService;
    private readonly CsvExportService _csvService;

    [ObservableProperty]
    private string _selectedReportType = "Invoices Summary";

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-3);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private decimal _reportTotalMetric;

    [ObservableProperty]
    private string _reportMetricLabel = "Total";

    public ObservableCollection<string> ReportTypes { get; } = new()
    {
        "Invoices Summary",
        "Payment History",
        "Outstanding Balances",
        "Overdue Invoices",
        "Customer-wise Outstanding"
    };

    public ObservableCollection<ReportRow> DisplayRows { get; } = new();

    public ReportsViewModel(
        AppDbContext context,
        InvoiceCalculationService calcService,
        CsvExportService csvService)
    {
        _context = context;
        _calcService = calcService;
        _csvService = csvService;
    }

    [RelayCommand]
    public async Task GenerateReportAsync()
    {
        DisplayRows.Clear();
        DateTime start = StartDate.Date;
        DateTime end = EndDate.Date.AddDays(1).AddTicks(-1);

        switch (SelectedReportType)
        {
            case "Invoices Summary":
                await GenerateInvoicesSummaryReportAsync(start, end);
                break;
            case "Payment History":
                await GeneratePaymentHistoryReportAsync(start, end);
                break;
            case "Outstanding Balances":
                await GenerateOutstandingBalancesReportAsync(start, end);
                break;
            case "Overdue Invoices":
                await GenerateOverdueInvoicesReportAsync(start, end);
                break;
            case "Customer-wise Outstanding":
                await GenerateCustomerOutstandingReportAsync();
                break;
        }
    }

    private async Task GenerateInvoicesSummaryReportAsync(DateTime start, DateTime end)
    {
        ReportMetricLabel = "Total Invoiced (Period)";
        var invs = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => i.IssueDate >= start && i.IssueDate <= end && i.Status != InvoiceStatus.Draft)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();

        ReportTotalMetric = invs.Sum(i => i.TotalAmount);

        foreach (var inv in invs)
        {
            decimal paid = _calcService.CalculatePaidAmount(inv);
            decimal balance = _calcService.CalculateOutstandingBalance(inv);

            DisplayRows.Add(new ReportRow
            {
                Col1 = inv.InvoiceNumber,
                Col2 = inv.Customer?.Name ?? "-",
                Col3 = inv.IssueDate.ToString("dd-MMM-yyyy"),
                Col4 = $"₹{inv.TotalAmount:N2}",
                Col5 = $"₹{paid:N2}",
                Col6 = $"₹{balance:N2} ({inv.Status})"
            });
        }
    }

    private async Task GeneratePaymentHistoryReportAsync(DateTime start, DateTime end)
    {
        ReportMetricLabel = "Total Collections (Period)";
        var payments = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i!.Customer)
            .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        ReportTotalMetric = payments.Sum(p => p.Amount);

        foreach (var p in payments)
        {
            DisplayRows.Add(new ReportRow
            {
                Col1 = p.Invoice?.InvoiceNumber ?? "-",
                Col2 = p.Invoice?.Customer?.Name ?? "-",
                Col3 = p.PaymentDate.ToString("dd-MMM-yyyy"),
                Col4 = $"₹{p.Amount:N2}",
                Col5 = p.PaymentMethod,
                Col6 = p.TransactionReference ?? "N/A"
            });
        }
    }

    private async Task GenerateOutstandingBalancesReportAsync(DateTime start, DateTime end)
    {
        ReportMetricLabel = "Total Outstanding (Active)";
        var invs = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Paid)
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        decimal totalBal = 0m;
        foreach (var inv in invs)
        {
            decimal balance = _calcService.CalculateOutstandingBalance(inv);
            if (balance > 0)
            {
                totalBal += balance;
                DisplayRows.Add(new ReportRow
                {
                    Col1 = inv.InvoiceNumber,
                    Col2 = inv.Customer?.Name ?? "-",
                    Col3 = inv.DueDate.ToString("dd-MMM-yyyy"),
                    Col4 = $"₹{inv.TotalAmount:N2}",
                    Col5 = $"₹{balance:N2}",
                    Col6 = inv.Status.ToString()
                });
            }
        }
        ReportTotalMetric = totalBal;
    }

    private async Task GenerateOverdueInvoicesReportAsync(DateTime start, DateTime end)
    {
        ReportMetricLabel = "Total Overdue Exposure";
        DateTime today = DateTime.Today;
        var invs = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => i.Status != InvoiceStatus.Draft)
            .ToListAsync();

        decimal totalOverdue = 0m;
        foreach (var inv in invs)
        {
            var status = _calcService.DetermineStatus(inv, today);
            if (status == InvoiceStatus.Overdue)
            {
                decimal balance = _calcService.CalculateOutstandingBalance(inv);
                int days = (today - inv.DueDate.Date).Days;
                totalOverdue += balance;

                DisplayRows.Add(new ReportRow
                {
                    Col1 = inv.InvoiceNumber,
                    Col2 = inv.Customer?.Name ?? "-",
                    Col3 = inv.DueDate.ToString("dd-MMM-yyyy"),
                    Col4 = $"₹{balance:N2}",
                    Col5 = $"{days} days overdue",
                    Col6 = "Action Required"
                });
            }
        }
        ReportTotalMetric = totalOverdue;
    }

    private async Task GenerateCustomerOutstandingReportAsync()
    {
        ReportMetricLabel = "Total Receivables Across Clients";
        var customers = await _context.Customers
            .Include(c => c.Invoices)
                .ThenInclude(i => i.Payments)
            .ToListAsync();

        decimal grandTotal = 0m;
        foreach (var cust in customers)
        {
            decimal custBal = cust.Invoices
                .Where(i => i.Status != InvoiceStatus.Draft)
                .Sum(i => _calcService.CalculateOutstandingBalance(i));

            if (custBal > 0)
            {
                grandTotal += custBal;
                int activeInvs = cust.Invoices.Count(i => i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Paid);

                DisplayRows.Add(new ReportRow
                {
                    Col1 = cust.Name,
                    Col2 = cust.Email,
                    Col3 = cust.Phone,
                    Col4 = $"{activeInvs} open invoice(s)",
                    Col5 = $"₹{custBal:N2}",
                    Col6 = "Active Receivables"
                });
            }
        }
        ReportTotalMetric = grandTotal;
    }

    [RelayCommand]
    public async Task ExportCsvAsync()
    {
        if (!DisplayRows.Any())
        {
            MessageBox.Show("No report data to export. Please generate a report first.", "Empty Report", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV (Comma delimited) (*.csv)|*.csv",
            FileName = $"{SelectedReportType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (saveDialog.ShowDialog() == true)
        {
            var headers = new[] { "Column 1", "Column 2", "Column 3", "Column 4", "Column 5", "Column 6" };
            var rows = DisplayRows.Select(r => new[] { r.Col1, r.Col2, r.Col3, r.Col4, r.Col5, r.Col6 });

            await _csvService.ExportToCsvAsync(saveDialog.FileName, headers, rows);
            MessageBox.Show($"Report successfully exported to:\n{saveDialog.FileName}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
