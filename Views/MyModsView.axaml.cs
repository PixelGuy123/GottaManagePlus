using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GottaManagePlus.Models;

namespace GottaManagePlus.Views;

public partial class MyModsView : UserControl
{
    public MyModsView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void HandleSizeChange(object? sender, SizeChangedEventArgs e)
    {
        // Handling Mod List names
        if (!e.WidthChanged) return;
        if (sender is not ItemsControl modList) return;
        
        foreach (ModItem? mod in modList.Items) 
            mod?.UpdateCutModName(e.NewSize.Width);
    }
}