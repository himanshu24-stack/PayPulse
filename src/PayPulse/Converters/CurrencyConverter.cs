using System;
using System.Globalization;
using System.Windows.Data;

namespace PayPulse.Converters;

public class CurrencyConverter : IValueConverter
{
    private static readonly CultureInfo IndiaCulture = new CultureInfo("en-IN");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            return $"₹{d.ToString("#,##,##0.00", IndiaCulture)}";
        }
        if (value is double dbl)
        {
            return $"₹{dbl.ToString("#,##,##0.00", IndiaCulture)}";
        }
        return "₹0.00";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
