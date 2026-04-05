using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using Microsoft.Extensions.Options;
using Serilog;
using AppSettingsContext = GottaManagePlus.Models.SourceGenerators.AppSettingsContext;

namespace GottaManagePlus.Services;

public sealed class SettingsService
{
    
    // Doesn't use an interface and I doubt this project would ever need a secondary configurations service
    public SettingsService(IOptions<AppSettings> initialOptions, ILogger logger)
    {
        _settings = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(initialOptions.Value, DefaultSerializerOptions), DefaultSerializerOptions)!;
        // Clamps the number in case the user changes manually in settings
        _settings.NumberOfRowsPerMod = Math.Clamp(_settings.NumberOfRowsPerMod, 4, 6);

        _logger = logger;
    }
    
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new() 
    { 
        WriteIndented = true,
        TypeInfoResolver = AppSettingsContext.Default // To optimize the serialization of AppSettings
    };
    
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.json");
    private readonly ILogger _logger;
    private readonly AppSettings _settings;
    
    public AppSettings CurrentSettings => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(_settings, DefaultSerializerOptions), DefaultSerializerOptions)!;
    
    public event Action? OnSaveSettings;

    public void UpdateSettings(Action<AppSettings> updateAction)
    {
        updateAction(_settings);
        OnSaveSettings?.Invoke();
    }

    public async Task<bool> Save()
    {
        _logger.Information("Saving settings...");
        var json = JsonSerializer.Serialize(_settings, DefaultSerializerOptions);
        StreamWriter? writer = null;
        try
        {
            writer = new StreamWriter(File.Open(_filePath, FileMode.OpenOrCreate));
            await writer.WriteAsync(json);
            _logger.Information("Settings successfully saved!");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("{exception}", ex);
            return false;
        }
        finally
        {
            if (writer != null)
                await writer.DisposeAsync();
        }
    }
}