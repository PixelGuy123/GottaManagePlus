using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class IdentifiableObject(int id) : ObservableObject
{
    [ObservableProperty]
    private int _id = id;
}