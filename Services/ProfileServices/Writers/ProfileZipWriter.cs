using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;
using SharpCompress.Common;
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
    
    // ----- Private -----
    private readonly ILogger _logger = logger;
    
    // ----- Public -----
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
    /// <returns>A new instance of <see cref="ProfileMetadata"/> that contains no storage data, or <see langword="null"/> if the writing failed.</returns>
    /// <exception cref="ArgumentException">If the path given is not a directory, this error is raised.</exception>
    public async Task<ProfileMetadata?> WriteEmptyProfileToAsync(string path, ProfileMetadata profile, GameEnvironmentController controller)
    {
        var clearedMetadata = new ProfileMetadata(profile, true);
        if (!await WriteProfileInternalAsync(path, clearedMetadata, controller, "Empty Profile Zip Writing started!",
                WriteEmptyProfileContent)) 
            return null;
        return clearedMetadata;
    }
    
    /// <summary>
    /// Common internal method for writing profiles with different content strategies.
    /// </summary>
    private async Task<bool> WriteProfileInternalAsync(string path, ProfileMetadata profile, GameEnvironmentController controller, 
        string startMessage, Action<IWriter> contentWriter)
    {
        var deletionSuffix = string.Concat("_ToDeletion_", Guid.NewGuid().ToString().AsSpan(0, 6));
        
        if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            throw new ArgumentException("Given path is not a directory.");

        _logger.Information("{startMessage}", startMessage);
        string desiredPath = Path.Combine(path, profile.Name), desiredDeletionPath = desiredPath + deletionSuffix;

        try
        {
            using var temporaryDirectory = controller.CreateTempSubdirectory(_logger);
            var profileRootDirectory = Directory.CreateDirectory(
                Path.Combine(temporaryDirectory.DirectoryInfo.FullName, profile.Name)
            );
            
            // --- Write compressed content ---
            var zipPathToWrite = Path.Combine(profileRootDirectory.FullName, $"{profile.Name}{Constants.ProfileDefaultExtension}");
            _logger.Information("Writing profile's content to '{zipPathToWrite}'...", zipPathToWrite);
            await using (var fileStream = File.OpenWrite(zipPathToWrite))
            {
                using var writer = WriterFactory.OpenWriter(fileStream, CompressedExtension,
                    WriterOptions.ForZip());

                // Does the content writing here
                contentWriter(writer);
            }
            
            // --- Write metadata file ---
            var metadataPath = Path.Combine(profileRootDirectory.FullName, Constants.ProfileMetadataFileName);
            _logger.Information("Writing metadata file to '{metadata}'...", metadataPath);
            File.WriteAllText(metadataPath, profile.Serialize());

            // Move final directory to target location
            _logger.Information("Moving base directory \'{ogPath}\' to \'{destPath}\'...", profileRootDirectory.FullName, desiredPath);
            
            // If the directory exists, rename to something temporary.
            if (Directory.Exists(desiredPath))
                Directory.Move(desiredPath, desiredDeletionPath);
            
            // Move the new directory to the path.
            profileRootDirectory.MoveTo(desiredPath);
            
            _logger.Information("Successfully written profile!");
            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to create the profile content.");
            
            // If the path exists, rename to original name.
            if (Directory.Exists(desiredDeletionPath))
                Directory.Move(desiredDeletionPath, desiredPath);
            return false;
        }
        finally
        {
            try
            {
                if (Directory.Exists(desiredDeletionPath))
                    Directory.Delete(desiredDeletionPath, true);
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
        _logger.Information("Writing live profile content for profile {ProfileName}", profile.Name);

        // Pre‑calculate total number of write operations
        int totalOps = 0, completed = 0;

        // Count asset directories
        foreach (var modData in profile.ModDataFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalOps += modData.Assets.Select(asset => controller.SearchAbsolutePath(asset.LocalPath)).Count(Directory.Exists);
        }

        // Count configs directory
        var configsDir = controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder);
        if (!string.IsNullOrEmpty(configsDir))
        {
            configsDir = controller.SearchAbsolutePath(configsDir);
            if (Directory.Exists(configsDir)) totalOps++;
        }

        // Count patchers directory
        var patchersDir = controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder);
        if (!string.IsNullOrEmpty(patchersDir))
        {
            patchersDir = controller.SearchAbsolutePath(patchersDir);
            if (Directory.Exists(patchersDir)) totalOps++;
        }
        
        // Count plugins directory
        var pluginsDir = controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PluginsFolder);
        if (!string.IsNullOrEmpty(pluginsDir))
        {
            pluginsDir = controller.SearchAbsolutePath(pluginsDir);
            if (Directory.Exists(pluginsDir)) totalOps++;
        }

        _logger.Information("Total write operations counted: {TotalOps}", totalOps);

        // Get the game root path once to compute correct relative paths inside the archive.
        var gameRoot = controller.CurrentEnvironment!.RootPath;

        // Write assets
        foreach (var modData in profile.ModDataFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var resourcePath in modData.Assets
                         .Select(asset => controller.SearchAbsolutePath(asset.LocalPath))
                         .Where(Directory.Exists))
            {
                cancellationToken.ThrowIfCancellationRequested();
                writer.WriteDirectory(resourcePath);
                completed++;
                _logger.Information("Compressed asset directory {Directory} ({Completed}/{TotalOps})",
                    resourcePath, completed, totalOps);
                progress?.Report(new ProgressReport(completed, totalOps, "Writing asset", Path.GetFileName(resourcePath)));
            }
        }
        
        // Write the whole Plugins folder
        TryToWriteDirectoryIfValid(writer, pluginsDir, gameRoot,
            ref completed, totalOps, null, controller, progress);

        // Write configs and patchers directories
        profile.ConfigurationFiles.Clear();
        TryToWriteDirectoryIfValid(writer, configsDir, gameRoot,
            ref completed, totalOps, profile.ConfigurationFiles, controller, progress);

        profile.PatcherFiles.Clear();
        TryToWriteDirectoryIfValid(writer, patchersDir, gameRoot,
            ref completed, totalOps, profile.PatcherFiles, controller, progress);

        _logger.Information("Finished writing live profile content. Total operations completed: {Completed}", completed);
        return;

        // Local helper that writes a directory and reports progress.
        void TryToWriteDirectoryIfValid(IWriter compressWriter, string? directoryPath, string gameRootPath,
            ref int completedItems, int totalOperations, List<string>? fileCollectionToAdd,
            GameEnvironmentController gameEnvironmentController, IProgress<ProgressReport>? progressReport)
        {
            if (string.IsNullOrEmpty(directoryPath)) return;

            directoryPath = gameEnvironmentController.SearchAbsolutePath(directoryPath);
            if (!Directory.Exists(directoryPath))
            {
                _logger.Warning("Directory does not exist, skipping: {DirectoryPath}", directoryPath);
                return;
            }

            _logger.Information("Writing directory contents: {DirectoryPath}", directoryPath);

            // Enumerate all files and write each one preserving the full relative path from the game root.
            foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                // Compute the relative path from the game root (e.g. "BepInEx/Plugins/MyPlugin/file.txt").
                var relativePath = Path.GetRelativePath(gameRootPath, file);

                compressWriter.Write(relativePath, file);
                fileCollectionToAdd?.Add(file);
            }

            completedItems++;
            _logger.Information("Compressed directory {Directory} ({Completed}/{TotalOps})",
                directoryPath, completedItems, totalOperations);
            progressReport?.Report(new ProgressReport(completedItems, totalOperations, "Writing directory", Path.GetFileName(directoryPath)));
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