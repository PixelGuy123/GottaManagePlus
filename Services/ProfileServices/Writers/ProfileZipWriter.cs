using System;
using System.IO;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Services.PlusFolderServices;
using SharpCompress.Common;
using ProgressReport = GottaManagePlus.Models.ProgressReport;

namespace GottaManagePlus.Services.ProfileServices.Writers;

public class ProfileZipWriter : DefaultProfileWriter
{
    protected override ArchiveType CompressedExtension => ArchiveType.Zip;
    protected override string FileExtension => ".zip";

    protected override Task FinalizeDirectory(DirectoryInfo rootDirectory, string path, ProfileMetadata profile, PlusFolderBrowser browser,
        IProgress<ProgressReport>? progress)
    {
        try
        {
            // Move final directory to target location
            var desiredPath = browser.SearchAbsolutePath(path);
            rootDirectory.MoveTo(desiredPath);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}