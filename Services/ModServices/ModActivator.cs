using System.Text.Json;
using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

public sealed class ModActivator(ILogger logger, GameEnvironmentController controller)
{
    // ---- Private ----
    private readonly ILogger _logger = logger;
    private readonly GameEnvironmentController _controller = controller;

    // ---- Public ----
    /// <summary>
    /// Activates or deactivates a mod by renaming its associated DLL files (plugins and patchers)
    /// and updating the mod's persistent metadata.
    /// </summary>
    /// <param name="manifest">The mod manifest representing the mod to activate or deactivate.</param>
    /// <param name="activate">
    /// <see langword="true"/> to activate the mod (rename .disabled → .dll);
    /// <see langword="false"/> to deactivate the mod (rename .dll → .disabled).
    /// </param>
    public void ToggleActivation(ModManifest manifest, bool activate)
    {
        _logger.Information("{Action} mod '{ModName}' (version {Version})",
            activate ? "Activating" : "Deactivating",
            manifest.Name,
            manifest.Version);

        try
        {
            // Rename plugin files
            var pluginDir = manifest.GetPluginDirectoryFromManifest(_controller);
            if (Directory.Exists(pluginDir))
            {
                foreach (var fullPath in manifest.Plugins
                             .Select(pluginRelativePath => Path.GetFileName(pluginRelativePath))
                             .Select(pluginFileName => Path.Combine(pluginDir, pluginFileName)))
                {
                    RenameDllFile(fullPath, activate);
                }
            }
            else
            {
                _logger.Warning("Plugin directory '{PluginDir}' does not exist. Skipping plugin rename.", pluginDir);
            }

            // Rename patcher files
            var patcherDir = _controller.SearchAbsolutePath(Constants.BepInExFolderName, 
                Constants.PatchersFolder);
            if (Directory.Exists(patcherDir))
            {
                foreach (var fullPath in manifest.Patchers
                             .Select(patcherRelativePath => Path.GetFileName(patcherRelativePath))
                             .Select(patcherFileName => Path.Combine(patcherDir, patcherFileName)))
                {
                    RenameDllFile(fullPath, activate);
                }
            }
            else
            {
                _logger.Warning("Patcher directory '{PatcherDir}' does not exist. Skipping patcher rename.", patcherDir);
            }

            // Update in-memory activation flag
            manifest.Metadata.Activated = activate;

            // Saves the metadata now.
            manifest.SaveMetadataToDisk(_controller, _logger);

            _logger.Information("Successfully {Action} mod '{ModName}'", activate ? "activated" : "deactivated", manifest.Name);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to {Action} mod '{ModName}'", activate ? "activate" : "deactivate", manifest.Name);
        }
    }

    /// <summary>
    /// Renames a single DLL file between .dll and .disabled based on the activation request.
    /// </summary>
    /// <param name="fullPath">Full filesystem path to the DLL file.</param>
    /// <param name="activate">
    /// <see langword="true"/> to rename a .disabled file back to .dll;
    /// <see langword="false"/> to rename a .dll file to .disabled.
    /// </param>
    private void RenameDllFile(string fullPath, bool activate)
    {
        if (!File.Exists(fullPath))
        {
            var disabledPath = Path.ChangeExtension(fullPath, Constants.PluginDisabledExtension);
            switch (activate)
            {
                case true when File.Exists(disabledPath):
                    // Activation: .disabled exists, rename to .dll
                    File.Move(disabledPath, fullPath);
                    _logger.Information("Renamed '{DisabledPath}' → '{DllPath}'", disabledPath, fullPath);
                    break;
                case false when File.Exists(fullPath):
                    // Deactivation: .dll exists, rename to .disabled
                    File.Move(fullPath, disabledPath);
                    _logger.Information("Renamed '{DllPath}' → '{DisabledPath}'", fullPath, disabledPath);
                    break;
                default:
                    // File not found in expected state; nothing to do
                    _logger.Warning("File '{FullPath}' not found in the expected state for {Action}.",
                        fullPath, activate ? "activation" : "deactivation");
                    break;
            }
        }
        else
        {
            // The .dll already exists; handle deactivation or ignore activation accordingly
            if (!activate)
            {
                var disabledPath = Path.ChangeExtension(fullPath, Constants.PluginDisabledExtension);
                File.Move(fullPath, disabledPath);
                _logger.Information("Renamed '{DllPath}' → '{DisabledPath}'", fullPath, disabledPath);
            }
            else
                _logger.Warning("File '{FullPath}' already exists as .dll, no rename needed for activation.", fullPath);
        }
    }
}