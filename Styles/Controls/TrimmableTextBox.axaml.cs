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