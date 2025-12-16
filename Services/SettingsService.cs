using System;
using System.IO;
using System.Text.Json;
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


    public void Save()
    {
        var json = JsonSerializer.Serialize(CurrentSettings, DefaultSerializerOptions);
        using var writer = new StreamWriter(File.Open(_filePath, FileMode.OpenOrCreate));
        writer.Write(json);
    }
}