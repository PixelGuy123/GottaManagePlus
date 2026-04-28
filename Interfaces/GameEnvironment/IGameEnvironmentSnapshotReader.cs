using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.GameEnvironment;

public interface IGameEnvironmentSnapshotReader
{
    EnvironmentSnapshot? ReadSnapshot(string indexFilePath);
}