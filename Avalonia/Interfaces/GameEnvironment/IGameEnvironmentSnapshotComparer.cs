using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentSnapshotComparer
{
    bool Compare(EnvironmentSnapshot current, EnvironmentSnapshot previous);
}