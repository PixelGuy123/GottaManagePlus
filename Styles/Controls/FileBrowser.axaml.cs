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

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace GottaManagePlus.Styles.Controls;

public class FileBrowser : TemplatedControl
{
    public static readonly StyledProperty<Dock?> ButtonAlignmentProperty =
        AvaloniaProperty.Register<FileBrowser, Dock?>(nameof(ButtonAlignment), defaultValue: Dock.Right, defaultBindingMode: BindingMode.OneWay);
    
    public static readonly StyledProperty<ICommand?> 
        SelectFileCommandProperty = 
            AvaloniaProperty.Register<FileBrowser, ICommand?>(nameof(SelectFileCommand), defaultBindingMode: BindingMode.OneWay),
        OpenFileCommandProperty = 
            AvaloniaProperty.Register<FileBrowser, ICommand?>(nameof(OpenFileCommand), defaultBindingMode: BindingMode.OneWay);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<FileBrowser, string?>(nameof(Label), "Pick a file:", defaultBindingMode: BindingMode.OneWay);
    
    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<FileBrowser, string?>(nameof(Placeholder), "No file selected...", defaultBindingMode: BindingMode.OneWay);
    
    public static readonly StyledProperty<string?> FilePathProperty =
        AvaloniaProperty.Register<FileBrowser, string?>(nameof(FilePath), defaultBindingMode: BindingMode.TwoWay);
    
    public static readonly StyledProperty<FontFamily?> LabelFontFamilyProperty = // Default font is Baskervville
        AvaloniaProperty.Register<FileBrowser, FontFamily?>(
            nameof(LabelFontFamily), 
            defaultValue: GetFontFamily("Baskerville"), 
            defaultBindingMode: BindingMode.OneWay);
    public static readonly StyledProperty<FontFamily?> PlaceholderFontFamilyProperty = // Default font is WorkSans
        AvaloniaProperty.Register<FileBrowser, FontFamily?>(
            nameof(PlaceholderFontFamily), 
            defaultValue: GetFontFamily("WorkSans"), 
            defaultBindingMode: BindingMode.OneWay);
    
    public Dock? ButtonAlignment
    {
        get => GetValue(ButtonAlignmentProperty); 
        set => SetValue(ButtonAlignmentProperty, value);
    }

    public ICommand? SelectFileCommand
    {
        get => GetValue(SelectFileCommandProperty);
        set => SetValue(SelectFileCommandProperty, value);
    }
    public ICommand? OpenFileCommand
    {
        get => GetValue(OpenFileCommandProperty);
        set => SetValue(OpenFileCommandProperty, value);
    }
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }
    public string? FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }
    public FontFamily? LabelFontFamily
    {
        get => GetValue(LabelFontFamilyProperty);
        set => SetValue(LabelFontFamilyProperty, value);
    }
    public FontFamily? PlaceholderFontFamily
    {
        get => GetValue(PlaceholderFontFamilyProperty);
        set => SetValue(PlaceholderFontFamilyProperty, value);
    }

    // ---- Private ----
    private static FontFamily? GetFontFamily(string fontName)
    {
        if (Application.Current?.TryGetResource(fontName, out var resource) == true)
            return resource as FontFamily;
    
        return FontFamily.Default; // Fallback
    }
}