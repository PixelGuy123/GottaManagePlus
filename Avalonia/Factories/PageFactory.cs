using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Factories;

/// <summary>
/// A factory responsible for getting the right <see cref="PageViewModel"/> from a <see cref="Func{Type, PageViewModel}"/>. 
/// </summary>
/// <param name="pageViewModelFactory">The constructor to be used.</param>
public class PageFactory(Func<Type, PageViewModel> pageViewModelFactory)
{
    public T GetPageViewModel<T>()
        where T : PageViewModel
    {
        var viewModel = (T)pageViewModelFactory(typeof(T));
        return viewModel;
    }
}