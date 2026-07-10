using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Models.DialogManagement;

namespace GottaManagePlus.ViewModels;

public abstract partial class DialogViewModel : ViewModelBase
{
    #region Properties

    [ObservableProperty]
    public partial bool IsDialogOpen { get; set; }

    #endregion

    #region Public Methods

    public async Task<object?> Show(DialogContext? context)
    {
        if (IsDialogOpen)
            return null;

        IsDialogOpen = true;
        var result = await OnShow(context);
        IsDialogOpen = false;
        return result;
    }

    #endregion

    #region Protected Abstract Methods

    protected abstract Task<object?> OnShow(DialogContext? context);

    #endregion
}