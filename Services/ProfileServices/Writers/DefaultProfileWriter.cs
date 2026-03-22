using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models.UI;
using System.IO;
using System.Linq;
using GottaManagePlus.Models;
using GottaManagePlus.Services.PlusFolderServices;
using GottaManagePlus.Utils;
using Serilog;
using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Writers;

using ProgressReport = GottaManagePlus.Models.ProgressReport; // Avoid ambiguity

namespace GottaManagePlus.Services.ProfileServices.Writers;

/// <summary>
/// A base writer that is responsible for writing <see cref="ProfileMetadata"/> to the local storage.
/// </summary>
public abstract class DefaultProfileWriter
{
    protected abstract ArchiveType CompressedExtension { get; }
    protected abstract string FileExtension { get; }
    
    /// <summary>
    /// Writes a <see cref="ProfileMetadata"/> into a compressed file type.
    /// </summary>
    /// <param name="path">The path this profile will be written at (MUST be a directory).</param>
    /// <param name="profile">The profile itself to be written.</param>
    /// <param name="browser">The file browser for preventing malicious paths from force-copying things outside the game folder.</param>
    /// <param name="progress">The progress to be reported</param>
    /// <exception cref="ArgumentException">If the path given is not a directory, this error is raised.</exception>
        public async Task WriteProfileTo(string path, ProfileMetadata profile, PlusFolderBrowser browser, IProgress<ProgressReport>? progress)
        {
            if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                throw new ArgumentException("Given path is not a directory.");

        // Profile Structure:
        // [ProfileName]
        //      [MetadataFile]
        //      [Profile.zip]
        
        /* What's inside a ProfileItem data?
         * Direct relative paths (from root) to the Configs and Patchers:
         * ./BepInEx/Patcher/...
         * ./BepInEx/Config/...
         * The ModMetadata is different, it gives three data sets:
         * Metadata's Path: ./BepInEx/Plugins/MyMod/.gmp/.metadata
         * 
         */
        DirectoryInfo? temporaryDirectory = null;

        try
        {
            temporaryDirectory = Directory.CreateTempSubdirectory($"GMP_{profile.Name}_{GetType().Name}");
            var profileRootDirectory = Directory.CreateDirectory(
                Path.Combine(temporaryDirectory.FullName, profile.Name)
            );

            // Write metadata file
            await using (var binaryWriter = new BinaryWriter(File.OpenWrite(
                             Path.Combine(profileRootDirectory.FullName, Constants.ProfileMetadataFileName))))
            {
                binaryWriter.Write(profile.Serialize());
            }

            // --- Pre‑calculate total number of write operations ---
            var totalOps = 0;
            
            foreach (var modData in profile.ModDataFiles ?? [])
            {
                // Count existing asset files
                totalOps += modData.Assets.Select(asset => browser.SearchAbsolutePath(asset.LocalPath)).Count(File.Exists);

                var pluginFolder = Path.GetDirectoryName(modData.Metadata.Path);
                // Count plugin directory (if it exists and is valid)
                if (string.IsNullOrEmpty(pluginFolder)) continue;
                
                var pluginPath = browser.SearchAbsolutePath(pluginFolder);
                if (Directory.Exists(pluginPath))
                    totalOps++;
            }
            
            // Count configs directory
            var configsDir = browser.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder);
            if (!string.IsNullOrEmpty(configsDir))
            {
                configsDir = browser.SearchAbsolutePath(configsDir);
                if (Directory.Exists(configsDir))
                    totalOps++;
            }
            
            // Count patchers directory
            var patchersDir = browser.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder);
            if (!string.IsNullOrEmpty(patchersDir))
            {
                patchersDir = browser.SearchAbsolutePath(patchersDir);
                if (Directory.Exists(patchersDir))
                    totalOps++;
            }
            
            // --- Write compressed content ---
            await using (var fileStream = File.OpenWrite(
                Path.Combine(profileRootDirectory.FullName, $"{profile.Name}{FileExtension}")))
            {
                var completed = 0;
                using var writer = WriterFactory.OpenWriter(fileStream, CompressedExtension,
                    new WriterOptions(CompressionType.LZMA, (int)CompressionLevel.BestSpeed));

                foreach (var modItem in profile.ModDataFiles ?? [])
                {
                    // Write assets
                    foreach (var resourcePath in modItem.Assets // If the asset exists in its local path, write it to the zip file following the same path.
                                 .Select(asset => 
                                     browser.SearchAbsolutePath(asset.LocalPath))
                                 .Where(Directory.Exists))
                    {
                        writer.WriteDirectory(resourcePath);
                        completed++;
                        progress?.Report(new ProgressReport(completed, totalOps, "Writing asset", Path.GetFileName(resourcePath)));
                    }

                    // Write plugin directory (as a single operation)
                    TryToWriteDirectoryIfValid(Path.GetDirectoryName(modItem.Metadata.Path), writer);
                }

                // Write configs and patchers directories
                TryToWriteDirectoryIfValid(configsDir, writer);
                TryToWriteDirectoryIfValid(patchersDir, writer);
    
                // Local helper that writes a directory and reports progress
                void TryToWriteDirectoryIfValid(string? directoryPath, IWriter compressWriter)
                {
                    if (string.IsNullOrEmpty(directoryPath)) return;
                    
                    directoryPath = browser.SearchAbsolutePath(directoryPath);
                    if (!Directory.Exists(directoryPath)) return;
                    
                    compressWriter.WriteAll(directoryPath, "*", SearchOption.AllDirectories);
                    completed++;
                    progress?.Report(new ProgressReport(completed, totalOps, "Writing directory", Path.GetFileName(directoryPath)));
                }
            }

            
            await FinalizeDirectory(profileRootDirectory, path, profile, browser, progress);
        }
        catch (Exception e)
        {
            Log.Logger.Error("Failed to create the profile content.\n{exception}", e);
        }
        finally
        {
            try
            {
                if (temporaryDirectory is { Exists: true })
                    temporaryDirectory.Delete(recursive: true);
            }
            catch 
            { 
                // suppress
            }
        }
    }
    
    // ----- Protected Methods -----
    protected abstract Task FinalizeDirectory(DirectoryInfo rootDirectory, string path, ProfileMetadata profile,
        PlusFolderBrowser browser, IProgress<ProgressReport>? progress);
}