using System;
using System.Collections.Generic;
using System.Text.Json;

namespace GottaManagePlus.Models.UI;

/// <summary>
/// Root object of a GameBanana index response containing metadata and a list of mod records.
/// </summary>
public class GameBananaIndex
{
    /// <summary>Metadata about the result set (total count, completeness, etc.).</summary>
    public IndexMetadata? Metadata { get; set; }

    /// <summary>List of mod records.</summary>
    public List<IndexMod> Records { get; set; } = [];

    /// <summary>
    /// Creates a <see cref="GameBananaIndex"/> from a JSON document.
    /// </summary>
    public static GameBananaIndex FromJson(JsonDocument doc)
    {
        var root = doc.RootElement;
        var index = new GameBananaIndex
        {
            Metadata = root.TryGetProperty("_aMetadata", out var meta) ? IndexMetadata.FromJson(meta) : null
        };

        if (!root.TryGetProperty("_aRecords", out var records)) return index;
        
        foreach (var recordElem in records.EnumerateArray())
            index.Records.Add(IndexMod.FromJson(recordElem));

        return index;
    }
}

/// <summary>
/// Metadata about the index result set (record count, completeness, pagination).
/// </summary>
public class IndexMetadata
{
    /// <summary>Total number of records available.</summary>
    public int RecordCount { get; set; }

    /// <summary>Whether the result set is complete (false if truncated).</summary>
    public bool IsComplete { get; set; }

    /// <summary>Number of records per page.</summary>
    public int PerPage { get; set; }

    /// <summary>
    /// Creates an <see cref="IndexMetadata"/> from a JSON element.
    /// </summary>
    public static IndexMetadata FromJson(JsonElement elem)
    {
        return new IndexMetadata
        {
            RecordCount = elem.TryGetProperty("_nRecordCount", out var count) ? count.GetInt32() : 0,
            IsComplete = elem.TryGetProperty("_bIsComplete", out var complete) && complete.GetBoolean(),
            PerPage = elem.TryGetProperty("_nPerpage", out var perPage) ? perPage.GetInt32() : 0
        };
    }
}

/// <summary>
/// A single mod record as returned in the GameBanana index.
/// </summary>
public class IndexMod
{
    /// <summary>Unique row ID of the mod.</summary>
    public int Id { get; set; }

    /// <summary>Model name (typically "Mod").</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Singular title ("Mod").</summary>
    public string SingularTitle { get; set; } = string.Empty;

    /// <summary>Icon CSS classes.</summary>
    public string IconClasses { get; set; } = string.Empty;

    /// <summary>Name of the mod.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL to the mod's profile page.</summary>
    public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>Unix timestamp when the mod was added.</summary>
    public DateTime DateAdded { get; set; }

    /// <summary>Unix timestamp when the mod was last modified.</summary>
    public DateTime DateModified { get; set; }

    /// <summary>Whether the mod has downloadable files.</summary>
    public bool HasFiles { get; set; }

    /// <summary>Payment type (e.g., "free").</summary>
    public string PayType { get; set; } = string.Empty;

    /// <summary>Tags associated with the mod.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Preview media (screenshots, etc.).</summary>
    public ModPreviewMedia? PreviewMedia { get; set; }

    /// <summary>Submitter (author) of the mod.</summary>
    public ModSubmitter? Submitter { get; set; }

    /// <summary>Game this mod belongs to.</summary>
    public ModGame? Game { get; set; }

    /// <summary>Root category (e.g., "Plus").</summary>
    public ModCategory? RootCategory { get; set; }

    /// <summary>Subcategory (e.g., "Plug-ins").</summary>
    public ModCategory? SubCategory { get; set; }

    /// <summary>Version string of the mod.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Whether the mod is obsolete.</summary>
    public bool IsObsolete { get; set; }

    /// <summary>Initial visibility state ("show", etc.).</summary>
    public string InitialVisibility { get; set; } = string.Empty;

    /// <summary>Whether content ratings exist.</summary>
    public bool HasContentRatings { get; set; }

    /// <summary>Number of likes.</summary>
    public int LikeCount { get; set; }

    /// <summary>Number of posts/comments.</summary>
    public int PostCount { get; set; }

    /// <summary>Whether the mod was featured.</summary>
    public bool WasFeatured { get; set; }

    /// <summary>Number of views.</summary>
    public int ViewCount { get; set; }

    /// <summary>Whether the current accessor owns the mod.</summary>
    public bool IsOwnedByAccessor { get; set; }

    /// <summary>
    /// Creates an <see cref="IndexMod"/> from a JSON element.
    /// </summary>
    public static IndexMod FromJson(JsonElement elem)
    {
        var mod = new IndexMod
        {
            Id = elem.TryGetProperty("_idRow", out var id) ? id.GetInt32() : 0,
            ModelName = elem.TryGetProperty("_sModelName", out var model) ? model.GetString() ?? string.Empty : string.Empty,
            SingularTitle = elem.TryGetProperty("_sSingularTitle", out var singular) ? singular.GetString() ?? string.Empty : string.Empty,
            IconClasses = elem.TryGetProperty("_sIconClasses", out var icon) ? icon.GetString() ?? string.Empty : string.Empty,
            Name = elem.TryGetProperty("_sName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            ProfileUrl = elem.TryGetProperty("_sProfileUrl", out var url) ? url.GetString() ?? string.Empty : string.Empty,
            HasFiles = elem.TryGetProperty("_bHasFiles", out var hasFiles) && hasFiles.GetBoolean(),
            PayType = elem.TryGetProperty("_sPayType", out var pay) ? pay.GetString() ?? "free" : "free",
            Version = elem.TryGetProperty("_sVersion", out var ver) ? ver.GetString() ?? string.Empty : string.Empty,
            IsObsolete = elem.TryGetProperty("_bIsObsolete", out var obsolete) && obsolete.GetBoolean(),
            InitialVisibility = elem.TryGetProperty("_sInitialVisibility", out var vis) ? vis.GetString() ?? "show" : "show",
            HasContentRatings = elem.TryGetProperty("_bHasContentRatings", out var ratings) && ratings.GetBoolean(),
            LikeCount = elem.TryGetProperty("_nLikeCount", out var likes) ? likes.GetInt32() : 0,
            PostCount = elem.TryGetProperty("_nPostCount", out var posts) ? posts.GetInt32() : 0,
            WasFeatured = elem.TryGetProperty("_bWasFeatured", out var featured) && featured.GetBoolean(),
            ViewCount = elem.TryGetProperty("_nViewCount", out var views) ? views.GetInt32() : 0,
            IsOwnedByAccessor = elem.TryGetProperty("_bIsOwnedByAccessor", out var owned) && owned.GetBoolean()
        };

        // Dates
        if (elem.TryGetProperty("_tsDateAdded", out var added))
            mod.DateAdded = UnixTimeStampToDateTime(added.GetInt64());
        if (elem.TryGetProperty("_tsDateModified", out var modified))
            mod.DateModified = UnixTimeStampToDateTime(modified.GetInt64());

        // Tags
        if (elem.TryGetProperty("_aTags", out var tags))
        {
            foreach (var tag in tags.EnumerateArray())
                mod.Tags.Add(tag.GetString() ?? string.Empty);
        }

        // Preview media
        if (elem.TryGetProperty("_aPreviewMedia", out var media))
            mod.PreviewMedia = ModPreviewMedia.FromJson(media);

        // Submitter
        if (elem.TryGetProperty("_aSubmitter", out var submitter))
            mod.Submitter = ModSubmitter.FromJson(submitter);

        // Game
        if (elem.TryGetProperty("_aGame", out var game))
            mod.Game = ModGame.FromJson(game);

        // Root Category
        if (elem.TryGetProperty("_aRootCategory", out var rootCat))
            mod.RootCategory = ModCategory.FromJson(rootCat);

        // Sub Category
        if (elem.TryGetProperty("_aSubCategory", out var subCat))
            mod.SubCategory = ModCategory.FromJson(subCat);

        return mod;
    }

    private static DateTime UnixTimeStampToDateTime(long unixTime)
        => DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
}

/// <summary>
/// Information about the mod submitter (author).
/// </summary>
public class ModSubmitter
{
    /// <summary>Unique row ID of the member.</summary>
    public int Id { get; set; }

    /// <summary>Member's display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the member is currently online.</summary>
    public bool IsOnline { get; set; }

    /// <summary>Whether the member has a "RIPE" badge.</summary>
    public bool HasRipe { get; set; }

    /// <summary>Profile URL of the member.</summary>
    public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>Avatar image URL.</summary>
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>Optional high-resolution avatar URL.</summary>
    public string? HdAvatarUrl { get; set; }

    /// <summary>Optional "upic" (user picture) URL.</summary>
    public string? UpicUrl { get; set; }

    /// <summary>Clearance levels (e.g., "Baldi Manager").</summary>
    public List<string> ClearanceLevels { get; set; } = [];

    /// <summary>
    /// Creates a <see cref="ModSubmitter"/> from a JSON element.
    /// </summary>
    public static ModSubmitter FromJson(JsonElement elem)
    {
        var submitter = new ModSubmitter
        {
            Id = elem.TryGetProperty("_idRow", out var id) ? id.GetInt32() : 0,
            Name = elem.TryGetProperty("_sName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            IsOnline = elem.TryGetProperty("_bIsOnline", out var online) && online.GetBoolean(),
            HasRipe = elem.TryGetProperty("_bHasRipe", out var ripe) && ripe.GetBoolean(),
            ProfileUrl = elem.TryGetProperty("_sProfileUrl", out var url) ? url.GetString() ?? string.Empty : string.Empty,
            AvatarUrl = elem.TryGetProperty("_sAvatarUrl", out var avatar) ? avatar.GetString() ?? string.Empty : string.Empty,
            HdAvatarUrl = elem.TryGetProperty("_sHdAvatarUrl", out var hd) ? hd.GetString() : null,
            UpicUrl = elem.TryGetProperty("_sUpicUrl", out var upic) ? upic.GetString() : null
        };

        if (elem.TryGetProperty("_aClearanceLevels", out var levels))
        {
            foreach (var level in levels.EnumerateArray())
                submitter.ClearanceLevels.Add(level.GetString() ?? string.Empty);
        }

        return submitter;
    }
}

/// <summary>
/// Information about the game associated with a mod.
/// </summary>
public class ModGame
{
    /// <summary>Unique row ID of the game.</summary>
    public int Id { get; set; }

    /// <summary>Game name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Profile URL of the game.</summary>
    public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>Icon URL for the game.</summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>
    /// Creates a <see cref="ModGame"/> from a JSON element.
    /// </summary>
    public static ModGame FromJson(JsonElement elem)
    {
        return new ModGame
        {
            Id = elem.TryGetProperty("_idRow", out var id) ? id.GetInt32() : 0,
            Name = elem.TryGetProperty("_sName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            ProfileUrl = elem.TryGetProperty("_sProfileUrl", out var url) ? url.GetString() ?? string.Empty : string.Empty,
            IconUrl = elem.TryGetProperty("_sIconUrl", out var icon) ? icon.GetString() ?? string.Empty : string.Empty
        };
    }
}

/// <summary>
/// A category (root or sub) of a mod.
/// </summary>
public class ModCategory
{
    /// <summary>Category name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Profile URL for the category.</summary>
    public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>Icon URL for the category.</summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>
    /// Creates a <see cref="ModCategory"/> from a JSON element.
    /// </summary>
    public static ModCategory FromJson(JsonElement elem)
    {
        return new ModCategory
        {
            Name = elem.TryGetProperty("_sName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            ProfileUrl = elem.TryGetProperty("_sProfileUrl", out var url) ? url.GetString() ?? string.Empty : string.Empty,
            IconUrl = elem.TryGetProperty("_sIconUrl", out var icon) ? icon.GetString() ?? string.Empty : string.Empty
        };
    }
}

/// <summary>
/// Preview media container (images, etc.) for a mod.
/// </summary>
public class ModPreviewMedia
{
    /// <summary>List of preview images.</summary>
    public List<ModImage> Images { get; set; } = [];

    /// <summary>
    /// Creates a <see cref="ModPreviewMedia"/> from a JSON element.
    /// </summary>
    public static ModPreviewMedia FromJson(JsonElement elem)
    {
        var media = new ModPreviewMedia();
        if (elem.TryGetProperty("_aImages", out var images))
        {
            foreach (var img in images.EnumerateArray())
                media.Images.Add(ModImage.FromJson(img));
        }
        return media;
    }

    /// <summary>Gets the URL of the first available thumbnail (220px wide).</summary>
    public string? GetThumbnailUrl() => Images.FirstOrDefault()?.GetThumbnailUrl();

    /// <summary>Gets the URL of the first available full image (530px wide).</summary>
    public string? GetImageUrl() => Images.FirstOrDefault()?.GetImageUrl();
}

/// <summary>
/// A single preview image (screenshot) belonging to a mod.
/// </summary>
public class ModImage
{
    /// <summary>Type of image (e.g., "screenshot").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Base URL for the image.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Caption text.</summary>
    public string Caption { get; set; } = string.Empty;

    /// <summary>File name of the original image.</summary>
    public string File { get; set; } = string.Empty;

    /// <summary>File name for 220px width thumbnail.</summary>
    public string? File220 { get; set; }

    /// <summary>Height of 220px thumbnail.</summary>
    public int? Height220 { get; set; }

    /// <summary>Width of 220px thumbnail.</summary>
    public int? Width220 { get; set; }

    /// <summary>File name for 530px width image.</summary>
    public string? File530 { get; set; }

    /// <summary>Height of 530px image.</summary>
    public int? Height530 { get; set; }

    /// <summary>Width of 530px image.</summary>
    public int? Width530 { get; set; }

    /// <summary>File name for 100px width thumbnail.</summary>
    public string? File100 { get; set; }

    /// <summary>Height of 100px thumbnail.</summary>
    public int? Height100 { get; set; }

    /// <summary>Width of 100px thumbnail.</summary>
    public int? Width100 { get; set; }

    /// <summary>Optional 800px width image.</summary>
    public string? File800 { get; set; }

    /// <summary>Height of 800px image.</summary>
    public int? Height800 { get; set; }

    /// <summary>Width of 800px image.</summary>
    public int? Width800 { get; set; }

    /// <summary>
    /// Creates a <see cref="ModImage"/> from a JSON element.
    /// </summary>
    public static ModImage FromJson(JsonElement elem)
    {
        return new ModImage
        {
            Type = elem.TryGetProperty("_sType", out var type) ? type.GetString() ?? string.Empty : string.Empty,
            BaseUrl = elem.TryGetProperty("_sBaseUrl", out var baseUrl) ? baseUrl.GetString() ?? string.Empty : string.Empty,
            Caption = elem.TryGetProperty("_sCaption", out var caption) ? caption.GetString() ?? string.Empty : string.Empty,
            File = elem.TryGetProperty("_sFile", out var file) ? file.GetString() ?? string.Empty : string.Empty,
            File220 = elem.TryGetProperty("_sFile220", out var f220) ? f220.GetString() : null,
            Height220 = elem.TryGetProperty("_hFile220", out var h220) ? h220.GetInt32() : null,
            Width220 = elem.TryGetProperty("_wFile220", out var w220) ? w220.GetInt32() : null,
            File530 = elem.TryGetProperty("_sFile530", out var f530) ? f530.GetString() : null,
            Height530 = elem.TryGetProperty("_hFile530", out var h530) ? h530.GetInt32() : null,
            Width530 = elem.TryGetProperty("_wFile530", out var w530) ? w530.GetInt32() : null,
            File100 = elem.TryGetProperty("_sFile100", out var f100) ? f100.GetString() : null,
            Height100 = elem.TryGetProperty("_hFile100", out var h100) ? h100.GetInt32() : null,
            Width100 = elem.TryGetProperty("_wFile100", out var w100) ? w100.GetInt32() : null,
            File800 = elem.TryGetProperty("_sFile800", out var f800) ? f800.GetString() : null,
            Height800 = elem.TryGetProperty("_hFile800", out var h800) ? h800.GetInt32() : null,
            Width800 = elem.TryGetProperty("_wFile800", out var w800) ? w800.GetInt32() : null
        };
    }

    /// <summary>Gets the full URL for the 220px thumbnail if available.</summary>
    public string? GetThumbnailUrl() => !string.IsNullOrEmpty(File220) ? CombineUrl(BaseUrl, File220) : null;

    /// <summary>Gets the full URL for the 530px image if available.</summary>
    public string? GetImageUrl() => !string.IsNullOrEmpty(File530) ? CombineUrl(BaseUrl, File530) : null;

    private static string CombineUrl(string baseUrl, string file) => $"{baseUrl.TrimEnd('/')}/{file}";
}