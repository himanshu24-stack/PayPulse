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

public partial class InvoicesViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly InvoiceCalculationService _calcService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    [ObservableProperty]
    private Invoice? _selectedInvoice;

    [ObservableProperty]
    private bool _isCreatingInvoice;

    // Invoice Form Fields
    [ObservableProperty]
    private Customer? _formSelectedCustomer;

    [ObservableProperty]
    private string _formInvoiceNumber = string.Empty;

    [ObservableProperty]
    private DateTime _formIssueDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _formDueDate = DateTime.Today.AddDays(30);

    [ObservableProperty]
    private string _formDescription = string.Empty;

    [ObservableProperty]
    private decimal _formDiscount = 0m;

    [ObservableProperty]
    private decimal _formTaxRate = 18m; // 18% GST

    [ObservableProperty]
    private decimal _calculatedSubtotal;

    [ObservableProperty]
    private decimal _calculatedTax;

    [ObservableProperty]
    private decimal _calculatedTotal;

    [ObservableProperty]
    private string _formItemDesc = string.Empty;

    [ObservableProperty]
    private decimal _formItemQty = 1m;

    [ObservableProperty]
    private decimal _formItemUnitPrice = 0m;

    [ObservableProperty]
    private string _formErrorMessage = string.Empty;

    public ObservableCollection<Invoice> Invoices { get; } = new();
    public ObservableCollection<Customer> AvailableCustomers { get; } = new();
    public ObservableCollection<InvoiceItem> FormItems { get; } = new();

    public InvoicesViewModel(AppDbContext context, InvoiceCalculationService calcService)
    {
        _context = context;
        _calcService = calcService;
    }

    [RelayCommand]
    public async Task LoadInvoicesAsync()
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string term = SearchText.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(term) ||
                                     (i.Customer != null && i.Customer.Name.ToLower().Contains(term)));
        }

        if (SelectedStatusFilter != "All")
        {
            if (Enum.TryParse<InvoiceStatus>(SelectedStatusFilter, out var filterStatus))
            {
                query = query.Where(i => i.Status == filterStatus);
            }
        }

        var list = await query.OrderByDescending(i => i.IssueDate).ToListAsync();

        DateTime today = DateTime.Today;
        Invoices.Clear();
        foreach (var inv in list)
        {
            inv.Status = _calcService.DetermineStatus(inv, today);
            Invoices.Add(inv);
        }

        if (SelectedInvoice == null && Invoices.Any())
        {
            SelectedInvoice = Invoices.First();
        }

        // Refresh Customers list
        var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
        AvailableCustomers.Clear();
        foreach (var c in customers)
        {
            AvailableCustomers.Add(c);
        }
    }

    partial void OnSearchTextChanged(string value) => _ = LoadInvoicesAsync();
    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadInvoicesAsync();

    [RelayCommand]
    public void OpenCreateInvoice()
    {
        IsCreatingInvoice = true;
        FormInvoiceNumber = $"INV-{DateTime.Now.Year}-{new Random().Next(100, 999)}";
        FormIssueDate = DateTime.Today;
        FormDueDate = DateTime.Today.AddDays(30);
        FormDescription = string.Empty;
        FormDiscount = 0m;
        FormTaxRate = 18m;
        FormItems.Clear();
        FormItemDesc = string.Empty;
        FormItemQty = 1m;
        FormItemUnitPrice = 0m;
        FormErrorMessage = string.Empty;
        RecalculateForm();
    }

    [RelayCommand]
    public void CancelCreateInvoice()
    {
        IsCreatingInvoice = false;
        FormErrorMessage = string.Empty;
    }

    [RelayCommand]
    public void AddLineItem()
    {
        if (string.IsNullOrWhiteSpace(FormItemDesc))
        {
            FormErrorMessage = "Item description cannot be empty.";
            return;
        }
        if (FormItemQty <= 0)
        {
            FormErrorMessage = "Quantity must be greater than 0.";
            return;
        }
        if (FormItemUnitPrice < 0)
        {
            FormErrorMessage = "Unit price cannot be negative.";
            return;
        }

        FormErrorMessage = string.Empty;
        FormItems.Add(new InvoiceItem
        {
            Description = FormItemDesc.Trim(),
            Quantity = FormItemQty,
            UnitPrice = FormItemUnitPrice,
            Amount = FormItemQty * FormItemUnitPrice
        });

        FormItemDesc = string.Empty;
        FormItemQty = 1m;
        FormItemUnitPrice = 0m;
        RecalculateForm();
    }

    [RelayCommand]
    public void RemoveLineItem(object? parameter)
    {
        if (parameter is InvoiceItem item)
        {
            FormItems.Remove(item);
            RecalculateForm();
        }
    }

    public void RecalculateForm()
    {
        decimal subtotal = FormItems.Sum(i => i.Amount);
        var result = _calcService.CalculateTotals(subtotal, FormDiscount, FormTaxRate);
        CalculatedSubtotal = result.Subtotal;
        CalculatedTax = result.TaxAmount;
        CalculatedTotal = result.TotalAmount;
    }

    partial void OnFormDiscountChanged(decimal value) => RecalculateForm();
    partial void OnFormTaxRateChanged(decimal value) => RecalculateForm();

    [RelayCommand]
    public async Task SaveInvoiceAsync()
    {
        FormErrorMessage = string.Empty;
        if (FormSelectedCustomer == null)
        {
            FormErrorMessage = "Please select a customer for this invoice.";
            return;
        }
        if (string.IsNullOrWhiteSpace(FormInvoiceNumber))
        {
            FormErrorMessage = "Invoice number is required.";
            return;
        }
        if (FormDueDate < FormIssueDate)
        {
            FormErrorMessage = "Due date cannot be before the issue date.";
            return;
        }
        if (!FormItems.Any())
        {
            FormErrorMessage = "Please add at least one line item to the invoice.";
            return;
        }

        bool exists = await _context.Invoices.AnyAsync(i => i.InvoiceNumber == FormInvoiceNumber.Trim());
        if (exists)
        {
            FormErrorMessage = "An invoice with this invoice number already exists.";
            return;
        }

        RecalculateForm();

        var invoice = new Invoice
        {
            InvoiceNumber = FormInvoiceNumber.Trim(),
            CustomerId = FormSelectedCustomer.Id,
            IssueDate = FormIssueDate,
            DueDate = FormDueDate,
            Description = string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription.Trim(),
            Subtotal = CalculatedSubtotal,
            Discount = FormDiscount,
            TaxRate = FormTaxRate,
            TaxAmount = CalculatedTax,
            TotalAmount = CalculatedTotal,
            Status = InvoiceStatus.Unpaid,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in FormItems)
        {
            invoice.Items.Add(new InvoiceItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount
            });
        }

        await _context.Invoices.AddAsync(invoice);
        await _context.SaveChangesAsync();

        IsCreatingInvoice = false;
        await LoadInvoicesAsync();
        SelectedInvoice = invoice;
    }

    [RelayCommand]
    public async Task DeleteInvoiceAsync(object? parameter)
    {
        var targetInvoice = parameter as Invoice ?? SelectedInvoice;
        if (targetInvoice == null) return;

        if (targetInvoice.Payments != null && targetInvoice.Payments.Any())
        {
            MessageBox.Show(
                "Invoices with recorded payments cannot be deleted directly. Delete the payments first to maintain audit integrity.",
                "Action Prevented",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var res = MessageBox.Show(
            $"Are you sure you want to delete invoice '{targetInvoice.InvoiceNumber}'?",
            "Confirm Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            _context.Invoices.Remove(targetInvoice);
            await _context.SaveChangesAsync();
            if (SelectedInvoice?.Id == targetInvoice.Id)
            {
                SelectedInvoice = null;
            }
            await LoadInvoicesAsync();
        }
    }
}
