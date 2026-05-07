using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GottaManagePlus.Models.UI;

/// <summary>
/// Concrete representation of a JSON document from <see cref="GottaManagePlus.Services.APIServices.GamebananaApiService.GetSubmissionDataAsync"/>.
/// </summary>
public partial class ModItem : ObservableObject
{
    [JsonPropertyName("_idRow")] public int Id { get; set; }

    [JsonPropertyName("_sName")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_sDescription")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("_aSubmitter")] public ModSubmitter? Submitter { get; set; }

    [JsonPropertyName("_aAdditionalInfo")] public ModAdditionalInfo? AdditionalInfo { get; set; }

    [JsonPropertyName("_sDownloadUrl")] public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("_aPreviewMedia")] public ModPreviewMedia? PreviewMedia { get; set; }

    // Helper properties for UI
    [JsonIgnore]
    public string Author
    {
        get => Submitter?.Name ?? "Unknown";
        set
        {
            Submitter?.Name = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public string Version
    {
        get => AdditionalInfo?.Version ?? "1.0";
        set
        {
            AdditionalInfo?.Version = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public string? ThumbnailUrl // By default, use the preview media if available; otherwise, whatever is set as field.
    {
        get => PreviewMedia?.GetThumbnailUrl() ?? field;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    [JsonIgnore]
    public string? ImageUrl // By default, use the preview media if available; otherwise, whatever is set as field.
    {
        get => PreviewMedia?.GetImageUrl() ?? field;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    [JsonIgnore]
    public partial bool IsSelected { get; set; }

    // Static factory from JsonDocument (for future API integration)
    public static ModItem FromJson(JsonDocument doc)
    {
        var root = doc.RootElement;
        return new ModItem
        {
            Id = root.TryGetProperty("_idRow", out var id) ? id.GetInt32() : 0,
            Name = root.TryGetProperty("_sName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            Description = root.TryGetProperty("_sDescription", out var desc)
                ? desc.GetString() ?? "No description"
                : "No description",
            DownloadUrl = root.TryGetProperty("_sDownloadUrl", out var url)
                ? url.GetString() ?? string.Empty
                : string.Empty,
            Submitter = root.TryGetProperty("_aSubmitter", out var submitter) ? ModSubmitter.FromJson(submitter) : null,
            AdditionalInfo = root.TryGetProperty("_aAdditionalInfo", out var info)
                ? ModAdditionalInfo.FromJson(info)
                : null,
            PreviewMedia = root.TryGetProperty("_aPreviewMedia", out var media) ? ModPreviewMedia.FromJson(media) : null
        };
    }

    // TODO: Add Image loading using AssetLoader when byte[] data is available
}

public class ModSubmitter
{
    [JsonPropertyName("_sName")] public string Name { get; set; } = string.Empty;

    public static ModSubmitter FromJson(JsonElement el) =>
        new()
        {
            Name = el.TryGetProperty("_sName", out var name) ? name.GetString() ?? "Unknown" : "Unknown"
        };
}

public class ModAdditionalInfo
{
    [JsonPropertyName("_sVersion")] public string Version { get; set; } = string.Empty;

    public static ModAdditionalInfo FromJson(JsonElement el) =>
        new()
        {
            Version = el.TryGetProperty("_sVersion", out var v) ? v.GetString() ?? "1.0" : "1.0"
        };
}

public class ModPreviewMedia
{
    [JsonPropertyName("_aImages")] public List<ModImage> Images { get; set; } = [];

    public static ModPreviewMedia FromJson(JsonElement el)
    {
        var media = new ModPreviewMedia();
        if (!el.TryGetProperty("_aImages", out var images)) return media;

        foreach (var img in images.EnumerateArray())
            media.Images.Add(ModImage.FromJson(img));

        return media;
    }

    public string? GetThumbnailUrl() => Images.FirstOrDefault()?.File100Url;
    public string? GetImageUrl() => Images.FirstOrDefault()?.File530Url;
}

public class ModImage
{
    [JsonPropertyName("_sBaseUrl")] public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("_sFile")] // Main File Image
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("_sFile100")] // Variation in different sizes
    public string File100 { get; set; } = string.Empty;

    [JsonPropertyName("_sFile530")] public string File530 { get; set; } = string.Empty;

    public string? File100Url =>
        string.IsNullOrEmpty(BaseUrl) || string.IsNullOrEmpty(File100) ? null : $"{BaseUrl}/{File100}";

    public string? File530Url =>
        string.IsNullOrEmpty(BaseUrl) || string.IsNullOrEmpty(File530) ? null : $"{BaseUrl}/{File530}";

    public static ModImage FromJson(JsonElement el) =>
        new()
        {
            BaseUrl = el.TryGetProperty("_sBaseUrl", out var url) ? url.GetString() ?? string.Empty : string.Empty,
            File = el.TryGetProperty("_sFile", out var file) ? file.GetString() ?? string.Empty : string.Empty,
            File100 = el.TryGetProperty("_sFile100", out var f100) ? f100.GetString() ?? string.Empty : string.Empty,
            File530 = el.TryGetProperty("_sFile530", out var f530) ? f530.GetString() ?? string.Empty : string.Empty
        };
}