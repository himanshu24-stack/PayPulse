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

public partial class PaymentsViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly InvoiceCalculationService _calcService;

    [ObservableProperty]
    private Payment? _selectedPayment;

    [ObservableProperty]
    private bool _isRecordingPayment;

    [ObservableProperty]
    private Invoice? _selectedInvoiceForPayment;

    [ObservableProperty]
    private decimal _paymentAmount;

    [ObservableProperty]
    private string _paymentMethod = "UPI";

    [ObservableProperty]
    private string _transactionRef = string.Empty;

    [ObservableProperty]
    private string _paymentNotes = string.Empty;

    [ObservableProperty]
    private decimal _invoiceTotal;

    [ObservableProperty]
    private decimal _invoiceAlreadyPaid;

    [ObservableProperty]
    private decimal _invoiceRemainingBalance;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<Payment> Payments { get; } = new();
    public ObservableCollection<Invoice> UnpaidInvoices { get; } = new();

    public PaymentsViewModel(AppDbContext context, InvoiceCalculationService calcService)
    {
        _context = context;
        _calcService = calcService;
    }

    [RelayCommand]
    public async Task LoadPaymentsAsync()
    {
        var list = await _context.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i!.Customer)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        Payments.Clear();
        foreach (var p in list)
        {
            Payments.Add(p);
        }

        if (SelectedPayment == null && Payments.Any())
        {
            SelectedPayment = Payments.First();
        }

        // Load invoices that can receive payments
        var invs = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => i.Status != InvoiceStatus.Draft)
            .ToListAsync();

        DateTime today = DateTime.Today;
        UnpaidInvoices.Clear();
        foreach (var inv in invs)
        {
            decimal balance = _calcService.CalculateOutstandingBalance(inv);
            if (balance > 0)
            {
                UnpaidInvoices.Add(inv);
            }
        }
    }

    partial void OnSelectedInvoiceForPaymentChanged(Invoice? value)
    {
        if (value != null)
        {
            InvoiceTotal = value.TotalAmount;
            InvoiceAlreadyPaid = _calcService.CalculatePaidAmount(value);
            InvoiceRemainingBalance = _calcService.CalculateOutstandingBalance(value);
            PaymentAmount = InvoiceRemainingBalance; // Default to full remaining balance
        }
        else
        {
            InvoiceTotal = 0;
            InvoiceAlreadyPaid = 0;
            InvoiceRemainingBalance = 0;
            PaymentAmount = 0;
        }
    }

    [RelayCommand]
    public void OpenRecordPayment()
    {
        IsRecordingPayment = true;
        ErrorMessage = string.Empty;
        TransactionRef = string.Empty;
        PaymentNotes = string.Empty;
        PaymentMethod = "UPI";

        if (UnpaidInvoices.Any())
        {
            SelectedInvoiceForPayment = UnpaidInvoices.First();
        }
    }

    [RelayCommand]
    public void CancelRecordPayment()
    {
        IsRecordingPayment = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task SavePaymentAsync()
    {
        ErrorMessage = string.Empty;
        if (SelectedInvoiceForPayment == null)
        {
            ErrorMessage = "Please select an invoice to record payment against.";
            return;
        }

        if (PaymentAmount <= 0)
        {
            ErrorMessage = "Payment amount must be greater than zero.";
            return;
        }

        decimal balance = _calcService.CalculateOutstandingBalance(SelectedInvoiceForPayment);
        if (PaymentAmount > balance)
        {
            ErrorMessage = $"Payment cannot exceed remaining balance (₹{balance:N2}).";
            return;
        }

        // Database transaction for payment recording and invoice status recalculation
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = new Payment
            {
                InvoiceId = SelectedInvoiceForPayment.Id,
                PaymentDate = DateTime.Today,
                Amount = PaymentAmount,
                PaymentMethod = PaymentMethod,
                TransactionReference = string.IsNullOrWhiteSpace(TransactionRef) ? null : TransactionRef.Trim(),
                Notes = string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();

            // Refresh invoice status
            var inv = await _context.Invoices
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == SelectedInvoiceForPayment.Id);

            inv.Status = _calcService.DetermineStatus(inv, DateTime.Today);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            IsRecordingPayment = false;
            await LoadPaymentsAsync();

            MessageBox.Show(
                $"Payment of ₹{PaymentAmount:N2} recorded successfully for {inv.InvoiceNumber}!",
                "Payment Recorded",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ErrorMessage = $"Error saving payment: {ex.Message}";
        }
    }
}
