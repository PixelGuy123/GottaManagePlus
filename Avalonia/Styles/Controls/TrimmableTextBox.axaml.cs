using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;

namespace GottaManagePlus.Styles.Controls;

[PseudoClasses(":focus-within", ":pointerover")]
public class TrimmableTextBox : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TrimmableTextBox, string?>(nameof(Text), defaultBindingMode: BindingMode.OneWay);
    
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<TrimmableTextBox, string?>(nameof(Watermark), defaultBindingMode: BindingMode.OneWay);

    public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
        AvaloniaProperty.Register<TrimmableTextBox, TextTrimming>(nameof(TextTrimming), TextTrimming.CharacterEllipsis);
    
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }
    
    static TrimmableTextBox()
    {
        // Tell the system to focusable so we can tab into it
        FocusableProperty.OverrideDefaultValue<TrimmableTextBox>(true);
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        // Select all when clicked
        var selectablePart = e.NameScope.Find<SelectableTextBlock>("PART_Content");
        if (selectablePart == null) return;
        
        // Add interaction features
        selectablePart.DoubleTapped += (_, _) => selectablePart.SelectAll();
    }
}