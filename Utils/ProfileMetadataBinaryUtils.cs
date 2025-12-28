using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using GottaManagePlus.Models;

namespace GottaManagePlus.Utils;

public static class ProfileMetadataBinaryUtils
{
    public const string ConfigsPrefix = "##Configs", PatchersPrefix = "##Patchers", ModsPrefix = "##Mods", ModsNamePrefix = ModsPrefix + "_Names", BinaryEndMarker = "::END";
    public static void WriteDirectoryStructure(this ProfileItem profile, BinaryWriter writer, IProgress<(int, int, string?)>? progress)
    {
        // Get all the values
        var maxAmount = profile.ConfigsMetaDataList.Count + 
                        profile.PatchersMetaDataList.Count +
                        profile.ModMetaDataList.Count;
        var currentAmount = 0;
        
        // Write all collections and end with a marker
        var status = "Saving configs to metadata...";
        ConvertCollectionToBinary(ConfigsPrefix, profile.ConfigsMetaDataList);
        status = "Saving patchers to metadata...";
        ConvertCollectionToBinary(PatchersPrefix, profile.PatchersMetaDataList);
        status = "Saving mods to metadata...";
        ConvertModCollectionToBinary(ModsPrefix, profile.ModMetaDataList);
        writer.Write(BinaryEndMarker);

        return;

        void ConvertCollectionToBinary(string prefix, ObservableCollection<ItemWithPath> collection)
        {
            // Get all files
            writer.Write(prefix);
        
            // Write count
            writer.Write(collection.Count);
        
            // Write content
            foreach (var itemWithPath in collection)
            {
                currentAmount++;
                writer.Write(!string.IsNullOrEmpty(itemWithPath.FullOsPath) ? itemWithPath.FullOsPath : string.Empty);
                progress?.Report((currentAmount, maxAmount, status));
            }
        }
        
        void ConvertModCollectionToBinary(string prefix, ObservableCollection<ModItem> collection)
        {
            // Get all files
            writer.Write(prefix);
        
            // Write count
            writer.Write(collection.Count);
        
            // Write content
            foreach (var itemWithPath in collection)
            {
                currentAmount++;
                writer.Write(!string.IsNullOrEmpty(itemWithPath.FullOsPath) ? itemWithPath.FullOsPath : string.Empty);
                progress?.Report((currentAmount, maxAmount, status));
            }

            // ### Mod Name Storage ###
            // Get all files
            writer.Write(prefix + ModsNamePrefix);
        
            // Write count
            writer.Write(collection.Count);
        
            // Write content
            foreach (var itemWithPath in collection)
            {
                currentAmount++;
                writer.Write(itemWithPath.ModName);
                progress?.Report((currentAmount, maxAmount, status));
            }
        }
    }

    public static Dictionary<string, string[]>? ReadDirectoryStructure(BinaryReader reader)
    {
        var cachedDictionary = new Dictionary<string, string[]>();
        var headerString = reader.ReadString();
        while (headerString != BinaryEndMarker)
        {
            // Read the collection length and create a new one
            var length = reader.ReadInt32();
            if (length > 500000) // This is not a normal amount for an array, this is skipped
            {
                Debug.WriteLine($"Large length detected: {length}.", Constants.DebugWarning);
                return null;
            }

            var files = new string[length];

            // Read serialized content of files
            for (var i = 0; i < files.Length; i++)
                files[i] = reader.ReadString();
            
            // Add the new item by header name
            cachedDictionary.Add(headerString, files);
            
            // Re-read the header again
            headerString = reader.ReadString();
        }

        return cachedDictionary;
    }
}