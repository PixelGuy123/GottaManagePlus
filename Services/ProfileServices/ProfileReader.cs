using System;
using System.Diagnostics;
using System.IO;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ProfileServices;

public static class ProfileReader
{
    public static ProfileItem? ReadProfile(string profileRootDirectory, int id)
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
            if (!metadataFile.Exists)
                throw new IOException("Metadata file is missing");

            // Try to read metadata
            ProfileMetadata? metadata;
            using (var binaryReader = new BinaryReader(metadataFile.OpenRead()))
            {
                // Should have at least one string
                metadata = ProfileMetadataUtils.ReadMetadata(binaryReader.ReadString());
            }

            if (metadata == null)
                throw new NullReferenceException("Metadata is null, it is either an invalid file or broken data.");

            // Return back a new instance of the ProfileItem
            return new ProfileItem(id, metadata)
            {
                FullOsPath = profileRootDirectory
            };
        }
        catch (Exception e)
        {
            Debug.WriteLine("Failed to read the profile content.", Constants.DebugError);
            Debug.WriteLine(e.ToString(), Constants.DebugError);
            return null;
        }
    }
}