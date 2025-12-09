using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GottaManagePlus.Modules.Utils;

namespace GottaManagePlus.Styles;

public class OutlinedTextBlock : TextBlock
{
    private Geometry? _textGeometry;
    private double _previousMaxWidth = -1, _previousMaxHeight = -1; // Optimization to not redraw geometry every single iteration
    
    public static readonly StyledProperty<IBrush?> OutlineColorProperty =
        AvaloniaProperty.Register<OutlinedTextBlock, IBrush?>(nameof(OutlineColor));

    public IBrush? OutlineColor
    {
        get => GetValue(OutlineColorProperty);
        set => SetValue(OutlineColorProperty, value);
    }
    
    public static readonly StyledProperty<double> OutlineWidthProperty =
        AvaloniaProperty.Register<OutlinedTextBlock, double>(nameof(OutlineWidth), 1d);

    public double OutlineWidth
    {
        get => GetValue(OutlineWidthProperty);
        set => SetValue(OutlineWidthProperty, value);
    }

    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        context.DrawGeometry(
            Foreground, 
            new Pen(OutlineColor, OutlineWidth), 
            GenerateGeometry(origin));
    }

    private Geometry GenerateGeometry(Point origin)
    {
        if (_textGeometry != null && 
            _previousMaxWidth.PreciseEquals(TextLayout.MaxWidth) &&
            _previousMaxHeight.PreciseEquals(TextLayout.MaxHeight))
            
            return _textGeometry;

        var formattedText = new FormattedText(
            Text ?? " ",
            CultureInfo.InvariantCulture,
            FlowDirection,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Foreground
        )
        {
            Trimming = TextTrimming,
            LineHeight = TextLayout.LineHeight,
            MaxLineCount = TextLayout.MaxLines + 1,
            TextAlignment = TextAlignment,
            MaxTextHeight = TextLayout.MaxHeight,
            MaxTextWidth = TextLayout.MaxWidth,
        };
        formattedText.SetFontFeatures(FontFeatures);
        if (TextDecorations != null)
            formattedText.SetTextDecorations(TextDecorations);

        _previousMaxWidth = TextLayout.MaxWidth;
        _previousMaxHeight = TextLayout.MaxHeight;
        
        _textGeometry = formattedText.BuildGeometry(origin);
        return _textGeometry!;
    }
}