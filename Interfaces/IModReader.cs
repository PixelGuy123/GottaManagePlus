using System.Collections.Generic;
using System.Threading.Tasks;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces;

// TODO: Reminder that ModMetadata is the essential local metadata for the mod.
// For things outside this, such as supported versions, needs to be written in specialized files
// that are going to be read through their names inside the JsonDocument.
public interface IModReader
{
    public ModItem? ExtractModStructure(string extractTo, ModMetadata metadata);
    public ModMetadata? LoadMetadataFile(string metadataPath);
    public List<string>? CheckForUnknownFileTypesInModStructure(ModItem modToAnalyze); // Security purposes
    public bool ValidateMetadataFile(string metadataPath);
}