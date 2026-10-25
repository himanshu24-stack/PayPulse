using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PayPulse.Data;

namespace PayPulse.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppDbContext _context;

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _databaseSize = string.Empty;

    [ObservableProperty]
    private string _businessName = "PayPulse Freelance Studio";

    [ObservableProperty]
    private string _gstNumber = "27AAAAA0000A1Z5";

    [ObservableProperty]
    private string _currencySymbol = "₹ (INR)";

    [ObservableProperty]
    private decimal _defaultTaxRate = 18m;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _selectedTable = "Customers";

    public ObservableCollection<string> AvailableTables { get; } = new()
    {
        "Customers",
        "Invoices",
        "InvoiceItems",
        "Payments",
        "ReminderLogs"
    };

    public ObservableCollection<object> TableRows { get; } = new();

    public SettingsViewModel(AppDbContext context)
    {
        _context = context;
        LoadDatabaseInfo();
        _ = LoadTableDataAsync();
    }

    partial void OnSelectedTableChanged(string value)
    {
        _ = LoadTableDataAsync();
    }

    [RelayCommand]
    public async Task LoadTableDataAsync()
    {
        TableRows.Clear();
        switch (SelectedTable)
        {
            case "Customers":
                var custs = await _context.Customers.AsNoTracking().ToListAsync();
                foreach (var c in custs) TableRows.Add(c);
                break;
            case "Invoices":
                var invs = await _context.Invoices.AsNoTracking().ToListAsync();
                foreach (var i in invs) TableRows.Add(i);
                break;
            case "InvoiceItems":
                var items = await _context.InvoiceItems.AsNoTracking().ToListAsync();
                foreach (var it in items) TableRows.Add(it);
                break;
            case "Payments":
                var payments = await _context.Payments.AsNoTracking().ToListAsync();
                foreach (var p in payments) TableRows.Add(p);
                break;
            case "ReminderLogs":
                var logs = await _context.ReminderLogs.AsNoTracking().ToListAsync();
                foreach (var l in logs) TableRows.Add(l);
                break;
        }
    }

    public void LoadDatabaseInfo()
    {
        DatabasePath = AppDbContext.GetDatabasePath();
        if (File.Exists(DatabasePath))
        {
            var info = new FileInfo(DatabasePath);
            DatabaseSize = $"{info.Length / 1024.0:N1} KB";
        }
        else
        {
            DatabaseSize = "Not created yet";
        }
    }

    [RelayCommand]
    public async Task ResetToSampleDataAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset the database to sample demonstration records? Existing custom records will be replaced.",
            "Confirm Demo Reset",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await DatabaseSeeder.SeedSampleDataAsync(_context, forceReset: true);
            LoadDatabaseInfo();
            StatusMessage = "Sample data has been re-seeded successfully!";
            MessageBox.Show("Sample data reset successfully!", "Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    public void OpenDatabaseFolder()
    {
        string dir = Path.GetDirectoryName(DatabasePath)!;
        if (Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }
    }
}
