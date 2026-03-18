using System;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.PlusFolderServices;

namespace GottaManagePlus.Services.ProfileServices.Extractors;

public abstract class ProfileExtractor : IProfileExtractor
{
    protected abstract string FileExtension { get; }
    
    public async Task<bool> ExtractProfile(ProfileItem profile, PlusFolderBrowser browser, IProgress<ProgressReport>? progress)
    {
        // Profile Structure:
        // [ProfileName]
        //      [MetadataFile]
        //      [Profile.zip]
    }
}