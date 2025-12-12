using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace GottaManagePlus.ViewModels;

public partial class DialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isDialogOpen;

    protected TaskCompletionSource CloseTask = new TaskCompletionSource();

    public async Task WaitAsync()
    {
        await CloseTask.Task;
    }

    public void Show()
    {
        if (CloseTask.Task.IsCompleted)
            CloseTask = new TaskCompletionSource();
        
        IsDialogOpen = true;
    }

    public void Close()
    {
        IsDialogOpen = false;

        CloseTask.TrySetResult();
    }
}