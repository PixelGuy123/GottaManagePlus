using System;

namespace GottaManagePlus.Interfaces;

public interface IGameFolderViewer
{
    bool ValidateFolder(string executablePath, bool setPathIfTrue = true);
    public Version GetGameVersion();
    public string? GetBaldiDataPath();
    public string? GetBepInExPath();
}