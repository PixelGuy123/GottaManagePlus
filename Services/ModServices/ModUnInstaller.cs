using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

public sealed class ModUnInstaller(ILogger logger, ProfileManager profileManager, GameEnvironmentController controller)
{
    // ---- Private API ----
    private readonly ILogger _logger = logger;
    private readonly ProfileManager _profileManager = profileManager;
    private readonly GameEnvironmentController _controller = controller;

    // ---- Public API ----
    public void DeleteMod(ModManifest manifest, Action<ProfileMetadata>? afterRemovalCallback = null)
    {
        // Get the active profile.
        var profile = _profileManager.ActiveProfile;
        if (profile == null)
        {
            _logger.Warning("Active profile is null.");
            return;
        }
        
        // Remove the manifest from the manager.
        profile.ModDataFiles.Remove(manifest);
        
        // Delete the mod file.
        Directory.Delete(manifest.GetPluginDirectoryFromManifest(_controller), true);
        
        // Callback if possible.
        afterRemovalCallback?.Invoke(profile);
    }
}