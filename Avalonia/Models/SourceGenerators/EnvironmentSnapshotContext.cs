using Tomlyn.Serialization;

namespace GottaManagePlus.Models.SourceGenerators;

[TomlSerializable(typeof(EnvironmentSnapshot))]
[TomlSerializable(typeof(EnvironmentSnapshot.SnapshotFileEntry))]
internal partial class EnvironmentSnapshotContext : TomlSerializerContext;