using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GottaManagePlus.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Debugging for getting manifest names if needed
#if DEBUG
        var names =
        System
        .Reflection
        .Assembly
        .GetExecutingAssembly()
        .GetManifestResourceNames();

        foreach (var name in names)
        {
            Debug.WriteLine(name);
        }
#endif
        InitializeComponent();
    }
}