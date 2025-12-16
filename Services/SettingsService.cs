using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Models.JsonContext;
using Microsoft.Extensions.Options;

namespace GottaManagePlus.Services;

public class SettingsService(IOptions<AppSettings> initialOptions)
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new() 
    { 
        WriteIndented = true,
        TypeInfoResolver = AppSettingsContext.Default // To optimize the serialization of AppSettings
    };
    
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.json");
    
    public AppSettings CurrentSettings { get; } = initialOptions.Value;


    public async Task<bool> Save()
    {
        // The wrapper will add that "AppSettings" section into the JSON
        var wrapper = new AppSettingsWrapper { AppSettings = CurrentSettings };
        var json = JsonSerializer.Serialize(wrapper, DefaultSerializerOptions);
        try
        {
            await using var writer = new StreamWriter(File.Open(_filePath, FileMode.OpenOrCreate));
            await writer.WriteAsync(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}