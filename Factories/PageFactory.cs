using System;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Factories;

public class PageFactory(Func<Type, PageViewModel> pageViewModelFactory)
{
    public T GetPageViewModel<T>(Action<T>? afterCreation = null)
        where T : PageViewModel
    {
        var viewModel = (T)pageViewModelFactory(typeof(T));
        afterCreation?.Invoke(viewModel);
        return viewModel;
    }
}