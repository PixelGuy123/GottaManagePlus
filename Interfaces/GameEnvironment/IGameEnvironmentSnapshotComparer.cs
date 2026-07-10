using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentSnapshotComparer
{
    bool Compare(EnvironmentSnapshot current, EnvironmentSnapshot previous);
}