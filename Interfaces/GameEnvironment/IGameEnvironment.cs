using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironment
{
    OsPath RootPath { get; }
    OsPath ExecutablePath { get; }
    OsPath UnityDataFolder { get; }
    WrappedGameVersion GameVersion { get; }
}