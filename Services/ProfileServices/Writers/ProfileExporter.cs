using System;
using System.IO;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Services.PlusFolderServices;

namespace GottaManagePlus.Services.ProfileServices.Writers;

public class ProfileExporter : ProfileZipWriter
{
    protected override string FileExtension { get; } = Constants.ExportedProfileExtension;

    protected override Task FinalizeDirectory(DirectoryInfo rootDirectory, string path, ProfileMetadata profile, PlusFolderBrowser browser,
        IProgress<ProgressReport>? progress)
    {
        // TODO: Ensure the whole rootDirectory's content is zipped around another layer of compression 
        throw new NotImplementedException();
    }
}