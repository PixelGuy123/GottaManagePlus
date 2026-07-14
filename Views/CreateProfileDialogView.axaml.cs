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
using Avalonia.Markup.Xaml;

namespace GottaManagePlus.Views;

public partial class CreateProfileDialogView : UserControl
{
    public CreateProfileDialogView()
    {
        InitializeComponent();
        CreateProfileBtn.PropertyChanged += SaveButtonOnPropertyChanged;
    }
    
    // Opacity update to not be specifically 0
    private void SaveButtonOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (sender is not Button button) return;
        // If opacity 0, change to something different
        if (args.Property.Name == nameof(Opacity) && (double?)args.NewValue <= 0.0d)
            button.Opacity = 0.25d;
    }
}