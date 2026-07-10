using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentSnapshotReader
{
    EnvironmentSnapshot? ReadSnapshot(string indexFilePath);
}