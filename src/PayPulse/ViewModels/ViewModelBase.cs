using CommunityToolkit.Mvvm.ComponentModel;

namespace PayPulse.ViewModels;

/// <summary>
/// Base class for all ViewModels in PayPulse, providing INotifyPropertyChanged support
/// via the Microsoft CommunityToolkit.Mvvm.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
