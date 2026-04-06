using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
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
public sealed class ProfileZipWriter(ILogger logger)
{
    // Const Fields
    private const ArchiveType CompressedExtension = ArchiveType.Zip;
    private const string FileExtension = ".zip";
    
    // ----- Private API -----
    private readonly ILogger _logger = logger;
    
    // ----- Public API -----
    /// <summary>
    /// Writes a <see cref="ProfileMetadata"/> into a compressed file type.
    /// </summary>
    /// <param name="path">The path this profile will be written at (MUST be a directory).</param>
    /// <param name="profile">The profile itself to be written.</param>
    /// <param name="controller">The environment controller for preventing malicious paths from force-copying things outside the game folder.</param>
    /// <param name="progress">The progress to be reported</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <exception cref="ArgumentException">If the path given is not a directory, this error is raised.</exception>
    public async Task WriteProfileToAsync(string path, ProfileMetadata profile, GameEnvironmentController controller, IProgress<ProgressReport>? progress, CancellationToken cancellationToken = default)
    {
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
        await WriteProfileInternalAsync(path, profile, controller, "Profile Zip Writing started!", 
            writer => WriteLiveProfileContent(writer, profile, controller, progress, cancellationToken));
    }
    
    /// <summary>
    /// Writes an empty <see cref="ProfileMetadata"/> into a compressed file type.
    /// </summary>
    /// <param name="path">The path this profile will be written at (MUST be a directory).</param>
    /// <param name="profile">The profile itself to be written.</param>
    /// <param name="controller">The environment controller for preventing malicious paths from force-copying things outside the game folder.</param>
    /// <returns>A new instance of <see cref="ProfileMetadata"/> that contains no storage data.</returns>
    /// <exception cref="ArgumentException">If the path given is not a directory, this error is raised.</exception>
    public async Task<ProfileMetadata> WriteEmptyProfileToAsync(string path, ProfileMetadata profile, GameEnvironmentController controller)
    {
        var clearedMetadata = new ProfileMetadata(profile, true);
        await WriteProfileInternalAsync(path, clearedMetadata, controller, "Empty Profile Zip Writing started!", 
            WriteEmptyProfileContent);
        return clearedMetadata;
    }
    
    /// <summary>
    /// Common internal method for writing profiles with different content strategies.
    /// </summary>
    private async Task WriteProfileInternalAsync(string path, ProfileMetadata profile, GameEnvironmentController controller, 
        string startMessage, Action<IWriter> contentWriter)
    {
        if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            throw new ArgumentException("Given path is not a directory.");

        _logger.Information("{startMessage}", startMessage);
        DirectoryInfo? temporaryDirectory = null;

        try
        {
            temporaryDirectory = Directory.CreateTempSubdirectory($"GMP_{profile.Name}_ProfileZipWriter");
            var profileRootDirectory = Directory.CreateDirectory(
                Path.Combine(temporaryDirectory.FullName, profile.Name)
            );
            
            // --- Write compressed content ---
            _logger.Information("Writing profile\'s content...");
            await using (var fileStream = File.OpenWrite(
                Path.Combine(profileRootDirectory.FullName, $"{profile.Name}{FileExtension}")))
            {
                using var writer = WriterFactory.OpenWriter(fileStream, CompressedExtension,
                    new WriterOptions(CompressionType.LZMA, (int)CompressionLevel.BestSpeed));

                // Does the content writing here
                contentWriter(writer);
            }
            
            // --- Write metadata file ---
            await using (var binaryWriter = new BinaryWriter(File.OpenWrite(
                             Path.Combine(profileRootDirectory.FullName, Constants.ProfileMetadataFileName))))
            {
                _logger.Information("Writing metadata file...");
                binaryWriter.Write(profile.Serialize());
            }

            // Move final directory to target location
            var desiredPath = controller.SearchAbsolutePath(path);
            profileRootDirectory.MoveTo(desiredPath);
            
            _logger.Information("Successfully written profile!");
        }
        catch (Exception e)
        {
            _logger.Error("Failed to create the profile content.\n{exception}", e);
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

    /// <summary>
    /// Writes the content for a full profile with mods, configs, and patchers.
    /// </summary>
    private void WriteLiveProfileContent(IWriter writer, ProfileMetadata profile, GameEnvironmentController controller, 
        IProgress<ProgressReport>? progress, CancellationToken cancellationToken)
    {
        // --- Pre‑calculate total number of write operations ---
        int totalOps = 0, completed = 0;
        
        foreach (var modData in profile.ModDataFiles ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Count existing asset files
            totalOps += modData.Assets.Select(asset => controller.SearchAbsolutePath(asset.LocalPath)).Count(File.Exists);

            var pluginFolder = Path.GetDirectoryName(modData.Metadata.Path);
            // Count plugin directory (if it exists and is valid)
            if (string.IsNullOrEmpty(pluginFolder)) continue;
            
            var pluginPath = controller.SearchAbsolutePath(pluginFolder);
            if (Directory.Exists(pluginPath))
            {
                totalOps++;
            }
        }
        
        // Count configs directory
        var configsDir = controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder);
        if (!string.IsNullOrEmpty(configsDir))
        {
            configsDir = controller.SearchAbsolutePath(configsDir);
            if (Directory.Exists(configsDir))
            {
                totalOps++;
            }
        }
        
        // Count patchers directory
        var patchersDir = controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder);
        if (!string.IsNullOrEmpty(patchersDir))
        {
            patchersDir = controller.SearchAbsolutePath(patchersDir);
            if (Directory.Exists(patchersDir))
            {
                totalOps++;
            }
        }

        foreach (var modItem in profile.ModDataFiles ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Write assets
            foreach (var resourcePath in modItem.Assets // If the asset exists in its local path, write it to the zip file following the same path.
                         .Select(asset => 
                             controller.SearchAbsolutePath(asset.LocalPath))
                         .Where(Directory.Exists))
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.WriteDirectory(resourcePath);
                completed++;
                _logger.Information("Compressed {completed} out of {totalOps} assets.", completed, totalOps);
                progress?.Report(new ProgressReport(completed, totalOps, "Writing asset", Path.GetFileName(resourcePath)));
            }

            // Write plugin directory (as a single operation)
            TryToWriteDirectoryIfValid(writer, Path.GetDirectoryName(modItem.Metadata.Path), ref completed, totalOps, null, controller, progress);
        }

        // Write configs and patchers directories
        profile.ConfigurationFiles.Clear();
        TryToWriteDirectoryIfValid(writer, configsDir, ref completed, totalOps, profile.ConfigurationFiles, controller, progress);

        profile.PatcherFiles.Clear();
        TryToWriteDirectoryIfValid(writer, patchersDir, ref completed, totalOps, profile.PatcherFiles, controller, progress);
        return;

        // Local helper that writes a directory and reports progress.
        static void TryToWriteDirectoryIfValid(IWriter compressWriter, string? directoryPath, ref int completed, int totalOps, 
            List<string>? fileCollectionToAdd, GameEnvironmentController controller, IProgress<ProgressReport>? progress)
        {
            if (string.IsNullOrEmpty(directoryPath)) return;
                
            directoryPath = controller.SearchAbsolutePath(directoryPath);
            if (!Directory.Exists(directoryPath)) return;
        
            // Get all the files from every collection and write it down
            foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                compressWriter.Write(file.Substring(directoryPath.Length), file);
                fileCollectionToAdd?.Add(file);
            }
            compressWriter.WriteAll(directoryPath, "*", SearchOption.AllDirectories);
            completed++;
            progress?.Report(new ProgressReport(completed, totalOps, "Writing directory", Path.GetFileName(directoryPath)));
        }
    }

    /// <summary>
    /// Writes the content for an empty profile with just directory structure.
    /// </summary>
    private void WriteEmptyProfileContent(IWriter writer)
    {
        // Write empty directories
        writer.WriteDirectory(Path.Combine(Constants.BepInExFolderName, Constants.PluginsFolder));
        writer.WriteDirectory(Path.Combine(Constants.BepInExFolderName, Constants.ConfigFolder));
        writer.WriteDirectory(Path.Combine(Constants.BepInExFolderName, Constants.PatchersFolder));
    }

    
}