using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileTypeChecker;
using FileTypeChecker.Types;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A service for scanning a mod's archive and detecting any suspicious files.
/// </summary>
public static class SecurityScanner
{
    public static async Task<List<SecurityIssue>> ScanAsync(string modRootPath, InstallationProgressManager progress,
        ModManifest metadata, CancellationToken cancellationToken = default)
    {
    }
}