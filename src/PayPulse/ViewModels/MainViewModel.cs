using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PayPulse.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardVm;
    private readonly CustomersViewModel _customersVm;
    private readonly InvoicesViewModel _invoicesVm;
    private readonly PaymentsViewModel _paymentsVm;
    private readonly RiskAnalysisViewModel _riskVm;
    private readonly ReportsViewModel _reportsVm;
    private readonly SettingsViewModel _settingsVm;

    [ObservableProperty]
    private ViewModelBase _currentView = null!;

    [ObservableProperty]
    private string _currentPageTitle = "Dashboard";

    [ObservableProperty]
    private string _activeSection = "Dashboard";

    [ObservableProperty]
    private string _financialYearText = GetCurrentFinancialYear();

    private static string GetCurrentFinancialYear()
    {
        var today = System.DateTime.Today;
        int startYear = today.Month >= 4 ? today.Year : today.Year - 1;
        int endYear = (startYear + 1) % 100;
        return $"Financial Year {startYear}-{endYear:D2}";
    }

    public MainViewModel(
        DashboardViewModel dashboardVm,
        CustomersViewModel customersVm,
        InvoicesViewModel invoicesVm,
        PaymentsViewModel paymentsVm,
        RiskAnalysisViewModel riskVm,
        ReportsViewModel reportsVm,
        SettingsViewModel settingsVm)
    {
        _dashboardVm = dashboardVm;
        _customersVm = customersVm;
        _invoicesVm = invoicesVm;
        _paymentsVm = paymentsVm;
        _riskVm = riskVm;
        _reportsVm = reportsVm;
        _settingsVm = settingsVm;

        // Default view
        NavigateToDashboard();
    }

    [RelayCommand]
    public void NavigateToDashboard()
    {
        CurrentView = _dashboardVm;
        CurrentPageTitle = "Financial Overview & Metrics";
        ActiveSection = "Dashboard";
        _ = _dashboardVm.LoadDashboardDataAsync();
    }

    [RelayCommand]
    public void NavigateToCustomers()
    {
        CurrentView = _customersVm;
        CurrentPageTitle = "Customer Directory & Accounts";
        ActiveSection = "Customers";
        _ = _customersVm.LoadCustomersAsync();
    }

    [RelayCommand]
    public void NavigateToInvoices()
    {
        CurrentView = _invoicesVm;
        CurrentPageTitle = "Invoice Management & Billing";
        ActiveSection = "Invoices";
        _ = _invoicesVm.LoadInvoicesAsync();
    }

    [RelayCommand]
    public void NavigateToPayments()
    {
        CurrentView = _paymentsVm;
        CurrentPageTitle = "Payment Receipts & Collections";
        ActiveSection = "Payments";
        _ = _paymentsVm.LoadPaymentsAsync();
    }

    [RelayCommand]
    public void NavigateToRisk()
    {
        CurrentView = _riskVm;
        CurrentPageTitle = "Payment Risk & Overdue Scoring";
        ActiveSection = "Risk";
        _ = _riskVm.LoadRiskAnalysisAsync();
    }

    [RelayCommand]
    public void NavigateToReports()
    {
        CurrentView = _reportsVm;
        CurrentPageTitle = "Financial Reports & CSV Export";
        ActiveSection = "Reports";
        _ = _reportsVm.GenerateReportAsync();
    }

    [RelayCommand]
    public void NavigateToSettings()
    {
        CurrentView = _settingsVm;
        CurrentPageTitle = "System Configuration & Database";
        ActiveSection = "Settings";
        _settingsVm.LoadDatabaseInfo();
    }
}
