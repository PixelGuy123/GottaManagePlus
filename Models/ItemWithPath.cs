using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models;

public partial class ItemWithPath(int id) : IdentifiableObject(id)
{
    [ObservableProperty]
    private string? _fullOsPath;

    public string FileName => string.IsNullOrEmpty(FullOsPath) ? "Undefined" : Path.GetFileName(FullOsPath);

    public override string ToString() => $"Path: {FullOsPath ?? "Undefined"}";
}