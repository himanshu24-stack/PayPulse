using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PayPulse.Data;
using PayPulse.Models;
using PayPulse.Services;

namespace PayPulse.ViewModels;

public partial class RiskAnalysisViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly RiskAnalysisService _riskService;
    private readonly InvoiceCalculationService _calcService;

    [ObservableProperty]
    private string _filterRiskLevel = "All";

    [ObservableProperty]
    private RiskAssessmentResult? _selectedRiskItem;

    [ObservableProperty]
    private int _highRiskCount;

    [ObservableProperty]
    private int _mediumRiskCount;

    [ObservableProperty]
    private int _lowRiskCount;

    [ObservableProperty]
    private decimal _highRiskExposure;

    public ObservableCollection<RiskAssessmentResult> RiskResults { get; } = new();
    public ObservableCollection<ReminderLog> ReminderHistory { get; } = new();

    public RiskAnalysisViewModel(
        AppDbContext context,
        RiskAnalysisService riskService,
        InvoiceCalculationService calcService)
    {
        _context = context;
        _riskService = riskService;
        _calcService = calcService;
    }

    [RelayCommand]
    public async Task LoadRiskAnalysisAsync()
    {
        var allInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => i.Status != InvoiceStatus.Draft)
            .ToListAsync();

        DateTime today = DateTime.Today;
        var results = new System.Collections.Generic.List<RiskAssessmentResult>();

        foreach (var inv in allInvoices)
        {
            var custInvoices = allInvoices.Where(i => i.CustomerId == inv.CustomerId).ToList();
            var assessment = _riskService.AssessInvoiceRisk(inv, custInvoices, today);
            results.Add(assessment);
        }

        HighRiskCount = results.Count(r => r.RiskLevel == RiskLevel.High);
        MediumRiskCount = results.Count(r => r.RiskLevel == RiskLevel.Medium);
        LowRiskCount = results.Count(r => r.RiskLevel == RiskLevel.Low);
        HighRiskExposure = results.Where(r => r.RiskLevel == RiskLevel.High).Sum(r => r.OutstandingAmount);

        RiskResults.Clear();
        var filtered = FilterRiskLevel switch
        {
            "High" => results.Where(r => r.RiskLevel == RiskLevel.High),
            "Medium" => results.Where(r => r.RiskLevel == RiskLevel.Medium),
            "Low" => results.Where(r => r.RiskLevel == RiskLevel.Low),
            _ => results
        };

        foreach (var item in filtered.OrderByDescending(r => r.RiskScore))
        {
            RiskResults.Add(item);
        }

        if (SelectedRiskItem == null && RiskResults.Any())
        {
            SelectedRiskItem = RiskResults.First();
        }

        await LoadRemindersAsync();
    }

    partial void OnFilterRiskLevelChanged(string value) => _ = LoadRiskAnalysisAsync();

    partial void OnSelectedRiskItemChanged(RiskAssessmentResult? value) => _ = LoadRemindersAsync();

    private async Task LoadRemindersAsync()
    {
        ReminderHistory.Clear();
        if (SelectedRiskItem == null) return;

        var logs = await _context.ReminderLogs
            .Where(r => r.InvoiceId == SelectedRiskItem.InvoiceId)
            .OrderByDescending(r => r.ReminderDate)
            .ToListAsync();

        foreach (var log in logs)
        {
            ReminderHistory.Add(log);
        }
    }

    [RelayCommand]
    public void CopyReminderMessage()
    {
        if (SelectedRiskItem == null) return;

        string message =
            $"Dear {SelectedRiskItem.CustomerName},\n\n" +
            $"This is a friendly reminder regarding Invoice #{SelectedRiskItem.InvoiceNumber} with an outstanding balance of ₹{SelectedRiskItem.OutstandingAmount:N2}, which is currently {SelectedRiskItem.DaysOverdue} days past due.\n\n" +
            $"We kindly request you to process this payment at your earliest convenience. If you have already remitted the amount, please share the transaction reference.\n\n" +
            $"Thank you,\nAccounts Team | PayPulse";

        Clipboard.SetText(message);
        MessageBox.Show("Reminder message copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    public async Task LogFollowUpCompletedAsync()
    {
        if (SelectedRiskItem == null) return;

        var reminder = new ReminderLog
        {
            InvoiceId = SelectedRiskItem.InvoiceId,
            ReminderDate = DateTime.Now,
            ReminderType = "Direct Follow-Up",
            Notes = $"Followed up regarding ₹{SelectedRiskItem.OutstandingAmount:N2} balance. Contacted customer representative.",
            Status = "Follow-up Completed"
        };

        await _context.ReminderLogs.AddAsync(reminder);
        await _context.SaveChangesAsync();

        await LoadRemindersAsync();
        MessageBox.Show("Follow-up action logged to database successfully!", "Recorded", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
