using System;
using System.IO;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;
using Serilog;
using SharpCompress.Readers;

namespace GottaManagePlus.Services.ProfileServices.Readers;

public sealed class ProfileExportReader(ILogger logger)
{
    // ---- Private API -----
    private readonly ILogger _logger = logger;
    
    // ---- Public API ----
    /// <summary>
    /// Reads a <c>.gmpProfile</c> file's metadata compressed in it.
    /// </summary>
    /// <param name="compressedProfilePath">The path to the file.</param>
    /// <returns>An instance of <see cref="ProfileMetadata"/> if the metadata is available in the profile; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">If the file does not exist.</exception>
    public ProfileMetadata? ReadExportedProfile(string compressedProfilePath)
    {
        // If the file does not exist, throw.
        if (!File.Exists(compressedProfilePath))
            throw new ArgumentException("Compressed profile does not exist.");

        try
        {
            // Attempt to read in-memory the file.
            // If the profile's directory exists, then open it to read.
            using var fileStream = File.OpenRead(compressedProfilePath);
            
            // Make the reader, then write the content to it.
            using var reader = ReaderFactory.OpenReader(fileStream);
            
            // Search for the metadata file.
            while (reader.MoveToNextEntry())
            {
                var entry = reader.Entry;
                if (entry.Key?.Equals(Constants.ProfileMetadataFileName, StringComparison.OrdinalIgnoreCase) != true) continue;
                
                // Found entry, now use a StreamReader to unpack it.
                using var entryStream = reader.OpenEntryStream();
                using var streamReader = new StreamReader(entryStream);
                    
                // Try to make a metadata object out of it.
                return ProfileMetadataUtils.ReadMetadata(streamReader.ReadToEnd());
            }

            // No metadata found, return null.
            return null;
        }
        catch (Exception e)
        {
            _logger.Error("Failed to read exported profile \'{profName}\'.\n{exception}", 
                Path.GetFileName(compressedProfilePath), e);
            return null;
        }
    }
}