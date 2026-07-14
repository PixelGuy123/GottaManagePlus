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

using Avalonia;
using Avalonia.Controls;

namespace GottaManagePlus.Views;

public partial class MultiLoadingDialogView : UserControl
{
    public MultiLoadingDialogView()
    {
        InitializeComponent();
        Status.PropertyChanged += OnStatusOnPropertyChanged;
    }

    private void OnStatusOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property.Name != nameof(Status.Text)) return;

        if (args.NewValue is string { Length: > 124 } text)
            Status.FontSize = MathF.Max(12, 18 - (text.Length - 123) * 0.2f);
        else
            Status.FontSize = 18d;
    }
}