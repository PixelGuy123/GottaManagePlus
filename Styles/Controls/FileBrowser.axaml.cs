using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.Data;
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