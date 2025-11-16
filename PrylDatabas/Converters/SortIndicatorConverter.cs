using System;
using System.Globalization;
using System.Windows.Data;

namespace PrylDatabas.Converters;

/// <summary>
/// Converts sort state (current sort column and direction) to a sort indicator symbol.
/// For use with MultiBinding: first value is SortBy, second value is SortAscending.
/// Parameter should be the column name (e.g., "Number", "Name").
/// </summary>
public class SortIndicatorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // values[0] = SortBy (string)
        // values[1] = SortAscending (bool)
        // parameter = column name to check
        
        if (values.Length < 2 || values[0] is not string currentSort || values[1] is not bool sortAscending || parameter is not string columnName)
            return string.Empty;

        if (currentSort != columnName)
            return string.Empty;

        return sortAscending ? "▲" : "▼";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
