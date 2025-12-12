using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Interfaces;

public interface IDialogProvider
{
    DialogViewModel? Dialog { get; set; }
}