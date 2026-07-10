using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;
using Tomlyn.Serialization;

namespace GottaManagePlus.Utils.SourceGenerators;

[TomlSerializable(typeof(EnvironmentSnapshot))]
[TomlSerializable(typeof(EnvironmentSnapshot.SnapshotFileEntry))]
internal partial class EnvironmentSnapshotContext : TomlSerializerContext;