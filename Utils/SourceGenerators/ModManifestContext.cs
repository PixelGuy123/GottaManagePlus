using System.Text.Json.Serialization;
using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;
using GottaManagePlus.Models.ModManagement;
using GottaManagePlus.Models.System;
using GottaManagePlus.Utils.Collections;

namespace GottaManagePlus.Utils.SourceGenerators;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ModManifest))]
[JsonSerializable(typeof(ModMetadata))]
[JsonSerializable(typeof(DestinedAsset))]
[JsonSerializable(typeof(OsPath))]
[JsonSerializable(typeof(AutoSortedList<WrappedGameVersion>))]
internal partial class ModManifestContext : JsonSerializerContext;