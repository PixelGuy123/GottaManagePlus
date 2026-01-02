using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FileTypeChecker;
using FileTypeChecker.Types;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Models.JsonContext;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services;

public class ModReader(PlusFolderViewer viewer) : IModReader
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        TypeInfoResolver = ModMetadataContext.Default
    };
    
    private readonly IGameFolderViewer _gameFolderViewer = viewer;
    
    // Public getters
    protected IGameFolderViewer GameFolderViewer => _gameFolderViewer ?? throw new NullReferenceException("GameFolderViewer is null.");


    public ModItem? ExtractModStructure(string extractTo, ModMetadata metadata)
    {
        throw new NotImplementedException();
    }

    public ModMetadata? LoadMetadataFile(string metadataPath)
    {
        throw new NotImplementedException();
    }

    public List<string>? CheckForUnknownFileTypesInModStructure(ModItem modToAnalyze)
    {
        if (modToAnalyze.MetaData == null) return null;

        List<string> unknownFiles = [];
        
        // Scan plugins
        foreach (var pluginFile in modToAnalyze.MetaData.Assets)
        {
            // Scan every resource linked to that instance
            foreach (var pluginRes in GetResourcesPaths(pluginFile))
            {
                var extension = Path.GetExtension(pluginRes);
                if (string.IsNullOrEmpty(extension)) // If the extension is unknown, this is already off
                {
                    unknownFiles.Add(pluginRes);
                    continue;
                }
                
                FileStream? fileStream = null;
                try
                {
                    // Get the stream
                    using (fileStream = File.OpenRead(pluginRes))
                    {
                        
                        // Check if the resource is recognizable from the checker
                        if (FileTypeValidator.IsTypeRecognizable(fileStream))
                        {
                            // If it is an executable, but not a dll, something's wrong
                            // Otherwise, if it is a Unix executable, this is also wrong
                            if (
                                FileTypeValidator.Is<Executable>(fileStream) && extension != ".dll" ||
                                FileTypeValidator.Is<ExecutableAndLinkableFormat>(fileStream)
                                )
                            {
                                unknownFiles.Add(pluginRes);
                            }
                        }
                    }
                }
                catch
                {
                    // Make sure the stream is at least disposed
                    fileStream?.Dispose();
                }
            }
        }

        // Scan assets
        foreach (var assetFile in modToAnalyze.MetaData.Assets)
        {
            // Scan every resource linked to that instance
            foreach (var assetRes in GetResourcesPaths(assetFile))
            {
                FileStream? fileStream = null;
                try
                {
                    // Get the stream
                    using (fileStream = File.OpenRead(assetRes))
                    {
                        // Check if the resource is recognizable from the checker
                        if (FileTypeValidator.IsTypeRecognizable(fileStream))
                        {
                            // If it is not an asset file, return false
                            if (
                                !FileTypeValidator.IsImage(fileStream) && // If not image
                                !FileCheckerUtils.IsAudio(fileStream) && // If not audio
                                !FileCheckerUtils.IsVideo(fileStream) // If not video
                                )
                            {
                                unknownFiles.Add(assetRes);
                            }
                        }
                    }
                }
                catch
                {
                    // Make sure the stream is at least disposed
                    fileStream?.Dispose();
                }
            }
        }
        
        return unknownFiles;
    }

    public bool ValidateMetadataFile(string metadataPath)
    {
        throw new NotImplementedException();
    }
    
    // Private members
    private static string[] GetResourcesPaths(ModMetadata.ModResourcesMetaData resource)
    {
        // If directory doesn't exist, this must be a single file
        return !Directory.Exists(resource.ResourcePath) ? 
            [resource.ResourcePath] : [.. ScanDirectoryAndGetFiles(resource.ResourcePath)];

        // Local recursive function
        static IEnumerable<string> ScanDirectoryAndGetFiles(string directory)
        {
            foreach (var file in Directory.GetFiles(directory))
                yield return file;
            foreach (var dir in Directory.GetDirectories(directory))
            {
                foreach (var file in ScanDirectoryAndGetFiles(dir))
                    yield return file;
            }
        }
    }
}