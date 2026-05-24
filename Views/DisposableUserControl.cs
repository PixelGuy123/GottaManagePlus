using Avalonia;
using Avalonia.Controls;

namespace GottaManagePlus.Views;

public class DisposableUserControl : UserControl
{
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is not IDisposable disposable) return;
        
        disposable.Dispose();
        OnDispose();
    }

    protected virtual void OnDispose() { }
}