/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

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