using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironment
{
    string RootPath { get; }
    string UnityDataFolder { get; }
    WrappedGameVersion GameVersion { get; }
}