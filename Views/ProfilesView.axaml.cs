using System;
using Avalonia;
using Avalonia.Controls;

namespace GottaManagePlus.Views;

public partial class ProfilesView : UserControl
{
    public ProfilesView()
    {
        InitializeComponent();
        // When detached, we dispose the profiles view
        DetachedFromVisualTree += (_, _) => 
        {
            if (DataContext is IDisposable disposable)
                disposable.Dispose();
        };
    }
}