using System;
using System.Globalization;
using Avalonia.Data.Converters;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Styles.Converters;

/// <summary>
/// Converts FilterTypes enum values to user-friendly display strings.
/// </summary>
public class FilterTypeToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ModSelectionDialogViewModel.FilterTypes filterType)
            return "Unknown";

        return filterType switch
        {
            ModSelectionDialogViewModel.FilterTypes.None => "Default Order",
            ModSelectionDialogViewModel.FilterTypes.NameAscending => "Name (A → Z)",
            ModSelectionDialogViewModel.FilterTypes.NameDescending => "Name (Z → A)",
            ModSelectionDialogViewModel.FilterTypes.AuthorAscending => "Author (A → Z)",
            ModSelectionDialogViewModel.FilterTypes.AuthorDescending => "Author (Z → A)",
            ModSelectionDialogViewModel.FilterTypes.DateAddedAscending => "Date Added (Oldest First)",
            ModSelectionDialogViewModel.FilterTypes.DateAddedDescending => "Date Added (Newest First)",
            ModSelectionDialogViewModel.FilterTypes.DateModifiedAscending => "Date Modified (Oldest First)",
            ModSelectionDialogViewModel.FilterTypes.DateModifiedDescending => "Date Modified (Newest First)",
            ModSelectionDialogViewModel.FilterTypes.DateUpdatedAscending => "Date Updated (Oldest First)",
            ModSelectionDialogViewModel.FilterTypes.DateUpdatedDescending => "Date Updated (Newest First)",
            ModSelectionDialogViewModel.FilterTypes.DownloadCountAscending => "Downloads (Lowest First)",
            ModSelectionDialogViewModel.FilterTypes.DownloadCountDescending => "Downloads (Highest First)",
            ModSelectionDialogViewModel.FilterTypes.ViewCountAscending => "Views (Lowest First)",
            ModSelectionDialogViewModel.FilterTypes.ViewCountDescending => "Views (Highest First)",
            _ => filterType.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}