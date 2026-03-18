using System;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.PlusFolderServices;

namespace GottaManagePlus.Interfaces;

public interface IProfileExtractor
{
    Task<bool> ExtractProfile(ProfileMetadata profile, string toPath, PlusFolderBrowser browser, IProgress<ProgressReport>? progress);
}