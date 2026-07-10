using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;
using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Writers;

namespace GottaManagePlus.Services.ProfileServices.Writers;

/// <summary>
/// A <c>.gmpProfile</c> generator for the profiles.
/// </summary>
public sealed class ProfileExporter(ILogger logger)
{
    public const ArchiveType ArchiveType = SharpCompress.Common.ArchiveType.Zip;
    
    // ----- Private -----
    private readonly ILogger _logger = logger;
    
    // ----- Public -----
    /// <summary>
    /// Exports a profile in the <c>.gmpProfile</c> format.
    /// </summary>
    /// <param name="exportPath">The path to export the profile to.</param>
    /// <param name="profile">The <see cref="ProfileMetadata"/> to be exported.</param>
    /// <param name="controller">The controller to safely search the desired export path.</param>
    /// <exception cref="IOException">Throws if the directory to the profile does not exist.</exception>
    public void ExportProfileTo(string exportPath, ProfileMetadata profile, GameEnvironmentController controller)
    {
        try
        {
            // Get the path of the profile.
            var physicalPath = profile.GetPhysicalPath(controller);

            // Get the profile's directory.
            var profileDir = new DirectoryInfo(physicalPath);
            if (!profileDir.Exists) throw new IOException("Profile directory does not exist.");
            
            // If the profile's directory exists, then zip it up in a custom extension.
            using var fileStream = File.OpenWrite(
                             (string)Path.Combine(exportPath, $"{profile.Name}{Constants.ExportedProfileExtension}"));
            
            // Make the writer, then write the content to it.
            using var writer = WriterFactory.OpenWriter(fileStream, ArchiveType,
                WriterOptions.ForZip());
            
            _logger.Information("Exporting profile to '{dir}'...", profileDir.FullName);
            // Write the directory to the zip file.
            writer.WriteAll(profileDir.FullName, "*", SearchOption.AllDirectories);
            _logger.Information("Successfully exported profile to '{dir}'", profileDir.FullName);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to export profile '{profName}' to '{path}'.", profile.Name, exportPath);
        }
    }
}