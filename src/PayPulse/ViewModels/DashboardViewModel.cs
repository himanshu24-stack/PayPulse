using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PayPulse.Data;
using PayPulse.Models;
using PayPulse.Services;

namespace PayPulse.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly InvoiceCalculationService _calcService;
    private readonly RiskAnalysisService _riskService;

    [ObservableProperty]
    private decimal _totalInvoiced;

    [ObservableProperty]
    private decimal _totalReceived;

    [ObservableProperty]
    private decimal _totalOutstanding;

    [ObservableProperty]
    private int _overdueInvoicesCount;

    [ObservableProperty]
    private int _activeCustomersCount;

    [ObservableProperty]
    private double _collectionRate;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<Invoice> RecentInvoices { get; } = new();
    public ObservableCollection<Invoice> OverdueInvoices { get; } = new();
    public ObservableCollection<RiskAssessmentResult> HighRiskInvoices { get; } = new();

    public DashboardViewModel(
        AppDbContext context,
        InvoiceCalculationService calcService,
        RiskAnalysisService riskService)
    {
        _context = context;
        _calcService = calcService;
        _riskService = riskService;
    }

    [RelayCommand]
    public async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        try
        {
            var invoices = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Payments)
                .ToListAsync();

            var activeInvoices = invoices.Where(i => i.Status != InvoiceStatus.Draft).ToList();

            TotalInvoiced = activeInvoices.Sum(i => i.TotalAmount);
            TotalReceived = activeInvoices.SelectMany(i => i.Payments).Sum(p => p.Amount);
            TotalOutstanding = activeInvoices.Sum(i => _calcService.CalculateOutstandingBalance(i));

            DateTime today = DateTime.Today;
            var overdueList = activeInvoices
                .Where(i => _calcService.DetermineStatus(i, today) == InvoiceStatus.Overdue)
                .OrderBy(i => i.DueDate)
                .ToList();

            OverdueInvoicesCount = overdueList.Count;
            ActiveCustomersCount = await _context.Customers.CountAsync();

            CollectionRate = TotalInvoiced > 0
                ? Math.Round((double)(TotalReceived / TotalInvoiced) * 100, 1)
                : 0.0;

            // Update status of all active invoices based on current date
            foreach (var inv in activeInvoices)
            {
                inv.Status = _calcService.DetermineStatus(inv, today);
            }

            RecentInvoices.Clear();
            foreach (var inv in invoices.OrderByDescending(i => i.IssueDate).Take(6))
            {
                RecentInvoices.Add(inv);
            }

            OverdueInvoices.Clear();
            foreach (var inv in overdueList.Take(5))
            {
                OverdueInvoices.Add(inv);
            }

            // High risk invoices
            HighRiskInvoices.Clear();
            foreach (var inv in overdueList)
            {
                var custInvoices = invoices.Where(i => i.CustomerId == inv.CustomerId).ToList();
                var risk = _riskService.AssessInvoiceRisk(inv, custInvoices, today);
                if (risk.RiskLevel == RiskLevel.High || risk.RiskLevel == RiskLevel.Medium)
                {
                    HighRiskInvoices.Add(risk);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
