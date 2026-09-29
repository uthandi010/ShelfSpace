using System.Globalization;

namespace ShelfSpace.App.Converters;

/// <summary>Compares the bound value (as a string) against ConverterParameter -
/// used to show/hide UI based on a status string, e.g.
/// IsVisible="{Binding Status, Converter={StaticResource EqualsConverter}, ConverterParameter=Pending}"</summary>
public class EqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
