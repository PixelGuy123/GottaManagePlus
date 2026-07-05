using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using GottaManagePlus.Models;
using Serilog;
using AppSettingsContext = GottaManagePlus.Utils.SourceGenerators.AppSettingsContext;

namespace GottaManagePlus.Services;

public sealed class SettingsService
{
    private readonly ILogger _logger;
    private readonly string _filePath;
    private readonly AppSettings _appSettings;

    /// <summary>
    /// Gets a snapshot of the currently loaded settings. 
    /// Modifications should be made via <see cref="Update(Action{AppSettings})"/> to ensure validation.
    /// </summary>
    public AppSettings.ReadonlyAppSettings CurrentSettings => new(_appSettings);

    public SettingsService(
        ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _filePath = (string)Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.json");
        
        _appSettings = LoadOrDefault();
        ApplyConstraints(_appSettings);
        UpdateTheme(_appSettings.Theme);
        
        _logger.Information("Settings loaded from {FilePath}", _filePath);
    }
    
    // ----- Private -----

    /// <summary>
    /// Loads settings from disk, falling back to DI options or default instance.
    /// </summary>
    private AppSettings LoadOrDefault()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize(json, AppSettingsContext.Default.AppSettings);
                if (loaded is not null)
                {
                    _logger.Debug("Successfully deserialized settings from disk");
                    return loaded;
                }
                _logger.Warning("Deserialization returned null; using fallback");
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load settings from disk; using fallback");
        }

        // Fallback
        return new AppSettings();
    }

    /// <summary>
    /// Applies business constraints (e.g., clamping) to ensure valid state.
    /// Call after loading or mutating settings.
    /// </summary>
    private static void ApplyConstraints(AppSettings settings)
    {
        settings.NumberOfRowsPerMod = Math.Clamp(settings.NumberOfRowsPerMod, 4, 6);
    }

    /// <summary>
    /// Sets up the theme on application startup based on saved settings.
    /// </summary>
    public void UpdateTheme(string theme)
    {
        Application.Current?.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        
        _logger.Information("Theme set to {Theme} on startup", theme);
    }

    /// <summary>
    /// Safely updates settings using a mutation action, then applies constraints.
    /// </summary>
    public void Update(Action<AppSettings> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
     
        Dispatcher.UIThread.Post(() =>
        {
            mutate(_appSettings);
            ApplyConstraints(_appSettings);
            _logger.Debug("Settings updated in memory");
        });
    }

    /// <summary>
    /// Persists current settings to disk using atomic write pattern.
    /// </summary>
    /// <returns>True if save succeeded; false otherwise.</returns>
    public async Task<bool> SaveAsync()
    {
        var tempPath = _filePath + ".tmp";
        try
        {
            _logger.Information("Saving settings to {FilePath}", _filePath);
            var json = JsonSerializer.Serialize(_appSettings, AppSettingsContext.Default.AppSettings);

            // write to temp file, then replace
            await File.WriteAllTextAsync(tempPath, json);
            File.Replace(tempPath, _filePath, null);

            _logger.Information("Settings successfully saved");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings");
            return false;
        }
        finally
        {
            // Deletes tempPath if available
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}