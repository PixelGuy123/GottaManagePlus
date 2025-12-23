using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Models.JsonContext;
using Microsoft.Extensions.Options;

namespace GottaManagePlus.Services;

public class SettingsService(IOptions<AppSettings> initialOptions) // Doesn't use an interface and I doubt this project would ever need a secondary configurations service
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new() 
    { 
        WriteIndented = true,
        TypeInfoResolver = AppSettingsContext.Default // To optimize the serialization of AppSettings
    };
    
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.json");
    
    public AppSettings CurrentSettings { get; } = initialOptions.Value;
    public event Action? OnSaveSettings;

    public async Task<bool> Save()
    {
        // The wrapper will add that "AppSettings" section into the JSON
        var wrapper = new AppSettingsWrapper { AppSettings = CurrentSettings };
        var json = JsonSerializer.Serialize(wrapper, DefaultSerializerOptions);
        StreamWriter? writer = null;
        try
        {
            OnSaveSettings?.Invoke(); // Invoke first, then save
            
            writer = new StreamWriter(File.Open(_filePath, FileMode.OpenOrCreate));
            await writer.WriteAsync(json);
            await writer.DisposeAsync();
            return true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.ToString(), Constants.DebugError);
            if (writer != null)
                await writer.DisposeAsync();
            return false;
        }
    }
}