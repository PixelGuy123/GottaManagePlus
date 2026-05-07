using GottaManagePlus.Models;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Readers;

public sealed class ProfileZipReader(ILogger logger)
{
    // ---- Private API -----
    private readonly ILogger _logger = logger;
    
    // ---- Public API ----
    /// <summary>
    /// Reads a directory of a profile and converts its metadata (if available) into a <see cref="ProfileMetadata"/>.
    /// The path shall never be prompted by a user.
    /// </summary>
    /// <param name="profileRootDirectory">The directory root to be scanned.</param>
    /// <returns>An instance of <see cref="ProfileMetadata"/> with its path defined.</returns>
    /// <exception cref="ArgumentException">If the path is not a directory, this exception is raised.</exception>
    /// <exception cref="NullReferenceException">If the metadata is null, this error is raised.</exception>
    public ProfileMetadata? ReadProfile(string profileRootDirectory)
    {
        // If the path is a file, throw an error
        if (!File.GetAttributes(profileRootDirectory).HasFlag(FileAttributes.Directory))
            throw new ArgumentException("Given path is not a directory.");

        // Directory must exist obviously
        if (!Directory.Exists(profileRootDirectory))
            throw new IOException("Directory is missing.");

        try
        {
            // Profile Structure:
            // [ProfileName] / << Method shall assume it is in here
            //      [MetadataFile]
            //      [Profile.zip]

            // Try to search for the metadata file
            var metadataFile = new FileInfo(Path.Combine(profileRootDirectory, Constants.ProfileMetadataFileName));

            // If the file is missing, throw an error
            return !metadataFile.Exists ? null :
                // Try to read metadata
                ProfileMetadataUtils.ReadMetadata(File.ReadAllText(metadataFile.FullName));
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to read the profile content.");
            return null;
        }
    }
}