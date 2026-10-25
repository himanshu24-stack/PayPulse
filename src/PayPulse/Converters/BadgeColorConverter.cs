using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PayPulse.Models;

namespace PayPulse.Converters;

public class StatusBadgeBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "";
        return status switch
        {
            "Paid" => new SolidColorBrush(Color.FromRgb(220, 252, 231)),      // light green #DCFCE7
            "Overdue" => new SolidColorBrush(Color.FromRgb(254, 226, 226)),   // light red #FEE2E2
            "PartiallyPaid" => new SolidColorBrush(Color.FromRgb(254, 243, 199)), // light amber #FEF3C7
            "Unpaid" => new SolidColorBrush(Color.FromRgb(224, 242, 254)),    // light blue #E0F2FE
            _ => new SolidColorBrush(Color.FromRgb(241, 245, 249))            // light slate #F1F5F9 (Draft)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusBadgeFgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "";
        return status switch
        {
            "Paid" => new SolidColorBrush(Color.FromRgb(22, 101, 52)),       // dark green #166534
            "Overdue" => new SolidColorBrush(Color.FromRgb(153, 27, 27)),    // dark red #991B1B
            "PartiallyPaid" => new SolidColorBrush(Color.FromRgb(146, 64, 14)), // dark amber #92400E
            "Unpaid" => new SolidColorBrush(Color.FromRgb(7, 89, 133)),      // dark blue #075985
            _ => new SolidColorBrush(Color.FromRgb(71, 85, 105))             // dark slate #475569
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RiskBadgeBgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string risk = value?.ToString() ?? "";
        return risk switch
        {
            "High" => new SolidColorBrush(Color.FromRgb(254, 226, 226)),     // #FEE2E2
            "Medium" => new SolidColorBrush(Color.FromRgb(254, 243, 199)),   // #FEF3C7
            _ => new SolidColorBrush(Color.FromRgb(220, 252, 231))           // #DCFCE7
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RiskBadgeFgConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string risk = value?.ToString() ?? "";
        return risk switch
        {
            "High" => new SolidColorBrush(Color.FromRgb(185, 28, 28)),     // #B91C1C
            "Medium" => new SolidColorBrush(Color.FromRgb(180, 83, 9)),    // #B45309
            _ => new SolidColorBrush(Color.FromRgb(21, 128, 61))           // #15803D
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
