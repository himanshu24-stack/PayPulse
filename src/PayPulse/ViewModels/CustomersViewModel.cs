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

namespace PayPulse.ViewModels;

public partial class CustomersViewModel : ViewModelBase
{
    private readonly AppDbContext _context;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string _formContact = string.Empty;

    [ObservableProperty]
    private string _formEmail = string.Empty;

    [ObservableProperty]
    private string _formPhone = string.Empty;

    [ObservableProperty]
    private string _formAddress = string.Empty;

    [ObservableProperty]
    private string _formNotes = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<Invoice> CustomerInvoices { get; } = new();

    public CustomersViewModel(AppDbContext context)
    {
        _context = context;
    }

    [RelayCommand]
    public async Task LoadCustomersAsync()
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string term = SearchText.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) ||
                                     c.Email.ToLower().Contains(term) ||
                                     c.Phone.Contains(term));
        }

        var list = await query.OrderBy(c => c.Name).ToListAsync();
        Customers.Clear();
        foreach (var c in list)
        {
            Customers.Add(c);
        }

        if (SelectedCustomer == null && Customers.Any())
        {
            SelectedCustomer = Customers.First();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadCustomersAsync();
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        CustomerInvoices.Clear();
        if (value != null)
        {
            var invs = _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.CustomerId == value.Id)
                .OrderByDescending(i => i.IssueDate)
                .ToList();

            foreach (var inv in invs)
            {
                CustomerInvoices.Add(inv);
            }
        }
    }

    [RelayCommand]
    public void OpenAddForm()
    {
        IsEditing = true;
        FormName = string.Empty;
        FormContact = string.Empty;
        FormEmail = string.Empty;
        FormPhone = string.Empty;
        FormAddress = string.Empty;
        FormNotes = string.Empty;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public void OpenEditForm()
    {
        if (SelectedCustomer == null) return;
        IsEditing = true;
        FormName = SelectedCustomer.Name;
        FormContact = SelectedCustomer.ContactPerson ?? string.Empty;
        FormEmail = SelectedCustomer.Email;
        FormPhone = SelectedCustomer.Phone;
        FormAddress = SelectedCustomer.BillingAddress ?? string.Empty;
        FormNotes = SelectedCustomer.Notes ?? string.Empty;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public void CancelForm()
    {
        IsEditing = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task SaveCustomerAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(FormName))
        {
            ErrorMessage = "Customer / Business Name is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(FormEmail) || !FormEmail.Contains("@"))
        {
            ErrorMessage = "A valid Email address is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(FormPhone))
        {
            ErrorMessage = "Phone number is required.";
            return;
        }

        if (SelectedCustomer != null && !string.IsNullOrEmpty(FormName) && SelectedCustomer.Name == FormName)
        {
            // Update
            SelectedCustomer.ContactPerson = FormContact;
            SelectedCustomer.Email = FormEmail.Trim();
            SelectedCustomer.Phone = FormPhone.Trim();
            SelectedCustomer.BillingAddress = FormAddress.Trim();
            SelectedCustomer.Notes = FormNotes.Trim();

            _context.Customers.Update(SelectedCustomer);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Add New
            var newCust = new Customer
            {
                Name = FormName.Trim(),
                ContactPerson = string.IsNullOrWhiteSpace(FormContact) ? null : FormContact.Trim(),
                Email = FormEmail.Trim(),
                Phone = FormPhone.Trim(),
                BillingAddress = string.IsNullOrWhiteSpace(FormAddress) ? null : FormAddress.Trim(),
                Notes = string.IsNullOrWhiteSpace(FormNotes) ? null : FormNotes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Customers.AddAsync(newCust);
            await _context.SaveChangesAsync();
            SelectedCustomer = newCust;
        }

        IsEditing = false;
        await LoadCustomersAsync();
    }

    [RelayCommand]
    public async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        bool hasInvoices = await _context.Invoices.AnyAsync(i => i.CustomerId == SelectedCustomer.Id);
        if (hasInvoices)
        {
            MessageBox.Show(
                "This customer cannot be deleted because there are existing invoices associated with their account. Please settle or delete associated invoices first.",
                "Action Prevented",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Are you sure you want to permanently delete customer '{SelectedCustomer.Name}'?",
            "Confirm Customer Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _context.Customers.Remove(SelectedCustomer);
            await _context.SaveChangesAsync();
            SelectedCustomer = null;
            await LoadCustomersAsync();
        }
    }
}
