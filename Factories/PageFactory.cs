using System;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Factories;

public class PageFactory(Func<Type, PageViewModel> pageViewModelFactory)
{
    public T GetPageViewModel<T>()
        where T : PageViewModel
    {
        var viewModel = (T)pageViewModelFactory(typeof(T));
        return viewModel;
    }
}