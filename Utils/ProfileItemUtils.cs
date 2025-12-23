using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;

namespace GottaManagePlus.Utils;

public static class ProfileItemUtils
{
    /// <summary>
    /// Converts a <see cref="ProfileItemMetaData"/> into a <see cref="ProfileItem"/> and adds it to the provided <see cref="collection"/>.
    /// </summary>
    /// <param name="metaData">The required <see cref="ProfileItemMetaData"/>.</param>
    /// <param name="collection">The collection to correctly identify the <see cref="ProfileItem"/> in the index.</param>
    /// <param name="viewer">The <see cref="IGameFolderViewer"/> to be used in specific game folder operations.</param>
    /// <returns>An instance of <see cref="ProfileItem"/>.</returns>
    public static ProfileItem ToProfileItem(this ProfileItemMetaData metaData, IList<ProfileItem> collection, IGameFolderViewer viewer)
    {
        var profileItem = new ProfileItem(collection.Count, metaData.ProfileName);
        var rootPath = viewer.GetGameRootPath();
        
        foreach (var path in metaData.AllUsedDirectoryPaths)
        {
            // If invalid directory name, skip
            if (!Directory.Exists(path))
            {
                Debug.WriteLine("Directory not found for path: " + path, Constants.DebugWarning);
                continue;
            }

            // If it has directory name, it has a directory itself
            var dirInfo = new DirectoryInfo(path);

            if (dirInfo.Parent?.Name != "BepInEx")
            {
                Debug.WriteLine("Path found is not from BepInEx: " + path, Constants.DebugWarning);
                continue;
            }
            
            // Hard coded directory check
            switch (dirInfo.Name.ToLower(CultureInfo.InvariantCulture))
            {
                case "patchers":
                    RetrievePatchersToProfileItem(path);
                    break;
                case "configs":
                    RetrieveConfigsToProfileItem(path);
                    break;
                case "plugins":
                    RetrieveModsToProfileItem(path);
                    break;
            }
        }
        
        collection.Add(profileItem);
        return profileItem;
        
        
        // Local helper methods
        void RetrievePatchersToProfileItem(string patchersPath)
        {
            foreach (var file in Directory.GetFiles(patchersPath))
            {
                profileItem.PatchersMetaDataList.Add(new ItemWithPath(profileItem.PatchersMetaDataList.Count)
                {
                    FullOsPath = file,
                    RelativeOsPath = Path.GetRelativePath(rootPath, file)
                });
            }
        }
        
        void RetrieveConfigsToProfileItem(string configsPath)
        {
            foreach (var file in Directory.GetFiles(configsPath))
            {
                profileItem.ConfigsMetaDataList.Add(new ItemWithPath(profileItem.ConfigsMetaDataList.Count)
                {
                    FullOsPath = file,
                    RelativeOsPath = Path.GetRelativePath(rootPath, file)
                });
            }
        }

        void RetrieveModsToProfileItem(string modsPath)
        {
            // TODO: Add special treatment to get the folders used by the ModPath.
        }
    }

    /// <summary>
    /// Converts a <see cref="ProfileItem"/> into a <see cref="ProfileItemMetaData"/>.
    /// </summary>
    /// <param name="profileItem">The required <see cref="ProfileItem"/>.</param>
    /// <returns>An instance of <see cref="ProfileItemMetaData"/>.</returns>
    public static ProfileItemMetaData ToMetaData(this ProfileItem profileItem)
    {
        HashSet<string> directoriesPaths = [];

        // Get configs
        foreach (var config in profileItem.ConfigsMetaDataList)
        {
            // If OS path is null, skip
            if (string.IsNullOrEmpty(config.FullOsPath)) continue;
            
            var directoryPath = Path.GetDirectoryName(config.FullOsPath);
            if (string.IsNullOrEmpty(directoryPath)) continue; // If the directory is null, skip
            
            directoriesPaths.Add(directoryPath);
        }
        
        // Get patchers metadata list
        foreach (var patcher in profileItem.PatchersMetaDataList)
        {
            // If OS path is null, skip
            if (string.IsNullOrEmpty(patcher.FullOsPath)) continue;
            
            var directoryPath = Path.GetDirectoryName(patcher.FullOsPath);
            if (string.IsNullOrEmpty(directoryPath)) continue; // If the directory is null, skip

            directoriesPaths.Add(directoryPath);
        }
        
        // Get mods metadata list
        foreach (var mod in profileItem.ModMetaDataList)
        {
            // If OS path is null, skip
            if (string.IsNullOrEmpty(mod.FullOsPath)) continue;
            
            var directoryPath = Path.GetDirectoryName(mod.FullOsPath);
            if (string.IsNullOrEmpty(directoryPath)) continue; // If the directory is null, skip

            directoriesPaths.Add(directoryPath);
        }

        return new ProfileItemMetaData
        {
            ProfileName = profileItem.ProfileName,
            AllUsedDirectoryPaths = [.. directoriesPaths]
        };
    }
}