/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ProfileServices.Management;

public class LocalProfileCloner(
    ProfileRepository repository,
    GameEnvironmentController controller
) : IProfileCloner
{
    // ---- Private ----
    private readonly ProfileRepository _repository = repository;
    private readonly GameEnvironmentController _controller = controller;

    // ---- Public ----
    public ProfileMetadata? CloneProfile(ProfileMetadata metadata, string newName)
    {
        // Get Profiles' folder.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath();
        
        // Get the metadata inside the storage.
        var metadataPath = metadata.GetPhysicalPath(_controller);
        
        // Get a copy of the metadata.
        var newMetadata = new ProfileMetadata(metadata, false)
        {
            Name = newName
        };
        
        // Registers the new metadata.
        if (!_repository.Add(newMetadata))
            return null;
        
        // Copy the directory under a new name.
        var newProfileDir = Directory.CreateDirectory((string)Path.Combine(profilesFolder, newName));
        foreach (var file in Directory.EnumerateFiles(metadataPath, "*", SearchOption.AllDirectories))
        {
            // Copy the file to the new directory.
            File.Copy(file, 
                (string)Path.Combine(newProfileDir.FullName, Path.GetFileName(file)), 
                true);
        }
        
        // Write the metadata to the new directory.
        File.WriteAllText(
            (string)Path.Combine(newProfileDir.FullName, Constants.ProfileMetadataFileName),
            newMetadata.Serialize());
        
        // Locate the compacted file and rename it.
        var zipFilePath = (string)Path.Combine(newProfileDir.FullName, metadata.Name + Constants.ProfileDefaultExtension);
        if (File.Exists(zipFilePath))
            File.Move(zipFilePath,
                (string)Path.Combine(newProfileDir.FullName,
                    newName + Constants.ProfileDefaultExtension));
        
        return newMetadata;
    }
}