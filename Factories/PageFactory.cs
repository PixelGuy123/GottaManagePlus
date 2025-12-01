using System;
using GottaManagePlus.ViewModels;

namespace GottaManagePlus.Factories;

public class PageFactory(Func<PageNames, PageViewModel> pageViewModelFactory)
{
    private readonly Func<PageNames, PageViewModel> _pageViewModelFactory = pageViewModelFactory;

    public PageViewModel GetPageViewModel(PageNames pageName) => _pageViewModelFactory(pageName);
}