using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using GottaManagePlus.Services.APIServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Models.UI;

/// <summary>
/// Represents a mod submission from the GameBanana API, mapping the JSON structure returned by <see cref="GamebananaApiService.GetSubmissionDataAsync"/>.
/// </summary>
public partial class ModItem : ObservableObject, IDisposable
{
    /// <summary>Gets the unique identifier of the mod (from _idRow).</summary>
    [JsonPropertyName("_idRow")] public int Id { get; init; }

    /// <summary>Gets or sets the display name of the mod (from _sName).</summary>
    [JsonPropertyName("_sName")] public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the short description of the mod (from _sDescription).</summary>
    [JsonPropertyName("_sDescription")] public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the submitter information (from _aSubmitter).</summary>
    [JsonPropertyName("_aSubmitter")] public ModSubmitter? Submitter { get; set; }

    /// <summary>Gets or sets the last modification date (Unix timestamp from _tsDateModified).</summary>
    [JsonPropertyName("_tsDateModified")] public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the last update date (Unix timestamp from _tsDateUpdated).</summary>
    [JsonPropertyName("_tsDateUpdated")] public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the creation date (Unix timestamp from _tsDateAdded).</summary>
    [JsonPropertyName("_tsDateAdded")] public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the total download count (from _nDownloadCount).</summary>
    [JsonPropertyName("_nDownloadCount")] public int? DownloadCount { get; set; }

    /// <summary>Gets or sets the total view count (from _nViewCount).</summary>
    [JsonPropertyName("_nViewCount")] public int? ViewCount { get; set; }

    /// <summary>Gets or sets the total like count (from _nLikeCount).</summary>
    [JsonPropertyName("_nLikeCount")] public int? LikeCount { get; set; }

    /// <summary>Gets or sets the primary download URL (from _sDownloadUrl).</summary>
    [JsonPropertyName("_sDownloadUrl")] public string DownloadUrl { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the Gamebanana URL page (from _sProfileUrl).</summary>
    [JsonPropertyName("_sProfileUrl")] public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the preview media (screenshots/videos) (from _aPreviewMedia).</summary>
    [JsonPropertyName("_aPreviewMedia")] public ModPreviewMedia? PreviewMedia { get; set; }

    /// <summary>Indicates whether the mod is private (from _bIsPrivate).</summary>
    [JsonPropertyName("_bIsPrivate")] public bool IsPrivate { get; set; }

    /// <summary>Indicates whether the mod is trashed/deleted (from _bIsTrashed).</summary>
    [JsonPropertyName("_bIsTrashed")] public bool IsTrashed { get; set; }

    /// <summary>Gets or sets the version string (from _sVersion).</summary>
    [JsonPropertyName("_sVersion")] public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the comments mode (e.g., "open", "closed") (from _sCommentsMode).</summary>
    [JsonPropertyName("_sCommentsMode")] public string CommentsMode { get; set; } = "open";

    /// <summary>Gets or sets the number of updates posted (from _nUpdatesCount).</summary>
    [JsonPropertyName("_nUpdatesCount")] public int UpdatesCount { get; set; }

    /// <summary>Indicates whether the mod has any updates (from _bHasUpdates).</summary>
    [JsonPropertyName("_bHasUpdates")] public bool HasUpdates { get; set; }

    /// <summary>Gets or sets the total number of to-dos (from _nAllTodosCount).</summary>
    [JsonPropertyName("_nAllTodosCount")] public int AllTodosCount { get; set; }

    /// <summary>Indicates whether the mod has any to-dos (from _bHasTodos).</summary>
    [JsonPropertyName("_bHasTodos")] public bool HasTodos { get; set; }

    /// <summary>Gets or sets the number of posts/comments (from _nPostCount).</summary>
    [JsonPropertyName("_nPostCount")] public int PostCount { get; set; }

    /// <summary>Gets or sets the list of tag strings (from _aTags).</summary>
    [JsonPropertyName("_aTags")] public List<ModTag> Tags { get; set; } = [];

    /// <summary>Indicates whether the current user created this mod (from _bCreatedBySubmitter).</summary>
    [JsonPropertyName("_bCreatedBySubmitter")] public bool CreatedBySubmitter { get; set; }

    /// <summary>Indicates whether the mod is a port from another game (from _bIsPorted).</summary>
    [JsonPropertyName("_bIsPorted")] public bool IsPorted { get; set; }

    /// <summary>Gets or sets the number of thanks received (from _nThanksCount).</summary>
    [JsonPropertyName("_nThanksCount")] public int ThanksCount { get; set; }

    /// <summary>Gets or sets the initial visibility setting (from _sInitialVisibility).</summary>
    [JsonPropertyName("_sInitialVisibility")] public string InitialVisibility { get; set; } = "show";

    /// <summary>Gets or sets the payment type (e.g., "free") (from _sPayType).</summary>
    [JsonPropertyName("_sPayType")] public string PayType { get; set; } = "free";

    /// <summary>Indicates whether a table of contents should be generated (from _bGenerateTableOfContents).</summary>
    [JsonPropertyName("_bGenerateTableOfContents")] public bool GenerateTableOfContents { get; set; }

    /// <summary>Gets or sets the detailed description HTML (from _sText).</summary>
    [JsonPropertyName("_sText")] public string Text { get; set; } = string.Empty;

    /// <summary>Indicates whether to show RIPE promo (from _bShowRipePromo).</summary>
    [JsonPropertyName("_bShowRipePromo")] public bool ShowRipePromo { get; set; }

    /// <summary>Indicates whether external links should be followed (from _bFollowLinks).</summary>
    [JsonPropertyName("_bFollowLinks")] public bool FollowLinks { get; set; }

    /// <summary>Indicates whether the current user has unliked this mod (from _bAccessorHasUnliked).</summary>
    [JsonPropertyName("_bAccessorHasUnliked")] public bool AccessorHasUnliked { get; set; }

    /// <summary>Indicates whether the current user has liked this mod (from _bAccessorHasLiked).</summary>
    [JsonPropertyName("_bAccessorHasLiked")] public bool AccessorHasLiked { get; set; }

    /// <summary>Indicates whether the current user has thanked this mod (from _bAccessorHasThanked).</summary>
    [JsonPropertyName("_bAccessorHasThanked")] public bool AccessorHasThanked { get; set; }

    /// <summary>Indicates whether the current user is subscribed to this mod (from _bAccessorIsSubscribed).</summary>
    [JsonPropertyName("_bAccessorIsSubscribed")] public bool AccessorIsSubscribed { get; set; }

    /// <summary>Gets or sets the subscription row ID for the current user (from _idAccessorSubscriptionRow).</summary>
    [JsonPropertyName("_idAccessorSubscriptionRow")] public int AccessorSubscriptionRowId { get; set; }

    /// <summary>Indicates whether advanced requirements exist (from _bAdvancedRequirementsExist).</summary>
    [JsonPropertyName("_bAdvancedRequirementsExist")] public bool AdvancedRequirementsExist { get; set; }

    /// <summary>Gets or sets the list of requirements as pairs of [0: name, 1: url, 2:... are the types] (from _aRequirements).</summary>
    [JsonPropertyName("_aRequirements")] public List<List<string>> Requirements { get; set; } = [];

    /// <summary>Gets or sets the list of active (non‑archived) files (from _aFiles).</summary>
    [JsonPropertyName("_aFiles")] public List<ModFile> Files { get; set; } = [];

    /// <summary>Gets or sets the list of archived files (from _aArchivedFiles).</summary>
    [JsonPropertyName("_aArchivedFiles")] public List<ModFile> ArchivedFiles { get; set; } = [];

    /// <summary>Gets or sets the structured credits groups (from _aCredits).</summary>
    [JsonPropertyName("_aCredits")] public List<ModCreditsGroup> Credits { get; set; } = [];

    
    // To dispose the Bitmap images stored in.
    public void Dispose()
    {
        ImageUrlAsImage?.Dispose();
        ThumbnailUrlAsImage?.Dispose();
    }

    // Helper properties for UI
    [JsonIgnore]
    public string Author
    {
        get => Submitter?.Name ?? "Unknown";
        init
        {
            Submitter?.Name = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public string? ThumbnailUrl
    {
        get => PreviewMedia?.GetThumbnailUrl() ?? field;
        init
        {
            field = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public string? ImageUrl
    {
        get => PreviewMedia?.GetImageUrl() ?? field;
        init
        {
            field = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public IEnumerable<ModFile> AllFiles => Files.Union(ArchivedFiles);
    [JsonIgnore]
    public IEnumerable<ModFile> AllValidFiles => Files.Union(ArchivedFiles).Where(f => f.IndexedFile is {
        HasGMPRoot: true
    });

    [JsonIgnore] [ObservableProperty] public partial ObservableCollection<ModFile> AllEnvironmentallyValidFiles { get; set; } = [];

    [JsonIgnore] public Bitmap? ImageUrlAsImage { get; private set; }
    [JsonIgnore] public Bitmap? ThumbnailUrlAsImage { get; private set; }

    [ObservableProperty] [JsonIgnore] public partial bool IsSelected { get; set; }

    [ObservableProperty] [JsonIgnore] public partial bool IsUnselectable { get; set; }
    
    public void UpdateEnvironmentallyValidFiles(GameEnvironmentController controller)
    {
        var envValidFiles = new List<ModFile>();
        foreach (var file in AllValidFiles)
        {
            // Get indexed file.
            var idxFile = file.IndexedFile;

            // Get version file and check its existence.
            var versionFile = idxFile?.FindFileByName(Constants.ModSupportForGameVersionPreviewFilePrefixName);
            if (versionFile == null)
            {
                // If it doesn't exist, assume it's a universally accepted file.
                envValidFiles.Add(file);
                continue;
            }

            // Get the accepted versions.
            var acceptedVersions = versionFile.Split('_');
            var targetVersion = controller.CurrentEnvironment!.GameVersion.ToString();
            
            // If it exists, check version related to the environment.
            if (acceptedVersions.Contains(targetVersion,
                    StringComparer.FromComparison(StringComparison.OrdinalIgnoreCase)))
                envValidFiles.Add(file);
        }

        AllEnvironmentallyValidFiles = new ObservableCollection<ModFile>(
            envValidFiles.OrderBy(f => System.Version.TryParse(f.Version, out var v) ? v : new Version("0.0.0")));
    }

    public override bool Equals(object? obj) =>
        obj is ModItem item && item.Id == Id;
    public override int GetHashCode() => Id;
    public static bool operator ==(ModItem? a, ModItem? b) => a?.Equals(b) == true;
    public static bool operator !=(ModItem? a, ModItem? b) => !(a == b);
    public override string ToString() => $"{Name} ({Version} | {Author})";

    /// <summary>
    /// Creates a <see cref="ModItem"/> from a JSON document.
    /// </summary>
    public static ModItem FromJson(JsonDocument doc)
    {
        var root = doc.RootElement;
        var item = new ModItem
        {
            // Basic info
            Id = root.TryGetProperty("_idRow", out var id) ? id.GetInt32() : 0,
        };
        item.Name = root.TryGetProperty("_sName", out var name) ? name.GetString() ?? string.Empty : string.Empty;
        item.Description = root.TryGetProperty("_sDescription", out var desc)
            ? desc.GetString() ?? "No description"
            : "No description";
        item.DownloadUrl = root.TryGetProperty("_sDownloadUrl", out var dl)
            ? dl.GetString() ?? string.Empty
            : string.Empty;
        item.ProfileUrl = root.TryGetProperty("_sProfileUrl", out var prUrl)
            ? prUrl.GetString() ?? string.Empty
            : string.Empty; // Submitter
        item.Submitter = root.TryGetProperty("_aSubmitter", out var sub) ? ModSubmitter.FromJson(sub) : null; // Preview media
        item.PreviewMedia = root.TryGetProperty("_aPreviewMedia", out var media) ? ModPreviewMedia.FromJson(media) : null; // Counts
        item.DownloadCount = root.TryGetProperty("_nDownloadCount", out var dlCount) ? dlCount.GetInt32() : 0;
        item.ViewCount = root.TryGetProperty("_nViewCount", out var view) ? view.GetInt32() : 0;
        item.LikeCount = root.TryGetProperty("_nLikeCount", out var like) ? like.GetInt32() : 0; // Booleans
        item.IsTrashed = root.TryGetProperty("_bIsTrashed", out var trash) && trash.GetBoolean();
        item.IsPrivate = root.TryGetProperty("_bIsPrivate", out var priv) && priv.GetBoolean();
        item.CreatedBySubmitter = root.TryGetProperty("_bCreatedBySubmitter", out var created) && created.GetBoolean();
        item.IsPorted = root.TryGetProperty("_bIsPorted", out var ported) && ported.GetBoolean();
        item.GenerateTableOfContents = root.TryGetProperty("_bGenerateTableOfContents", out var toc) && toc.GetBoolean();
        item.ShowRipePromo = root.TryGetProperty("_bShowRipePromo", out var ripe) && ripe.GetBoolean();
        item.FollowLinks = root.TryGetProperty("_bFollowLinks", out var follow) && follow.GetBoolean();
        item.AccessorHasUnliked = root.TryGetProperty("_bAccessorHasUnliked", out var unliked) && unliked.GetBoolean();
        item.AccessorHasLiked = root.TryGetProperty("_bAccessorHasLiked", out var liked) && liked.GetBoolean();
        item.AccessorHasThanked = root.TryGetProperty("_bAccessorHasThanked", out var thanked) && thanked.GetBoolean();
        item.AccessorIsSubscribed = root.TryGetProperty("_bAccessorIsSubscribed", out var subbed) && subbed.GetBoolean();
        item.AdvancedRequirementsExist = root.TryGetProperty("_bAdvancedRequirementsExist", out var advReq) &&
                                         advReq.GetBoolean(); // Dates
        item.DateModified = root.TryGetProperty("_tsDateModified", out var mod) ? mod.GetUnixDateTime() : default;
        item.DateUpdated = root.TryGetProperty("_tsDateUpdated", out var upd) ? upd.GetUnixDateTime() : default;
        item.DateAdded = root.TryGetProperty("_tsDateAdded", out var added) ? added.GetUnixDateTime() : default; // Strings and simple values
        item.Version = root.TryGetProperty("_sVersion", out var ver) ? ver.GetString() ?? string.Empty : string.Empty;
        item.CommentsMode = root.TryGetProperty("_sCommentsMode", out var cmtMode)
            ? cmtMode.GetString() ?? "open"
            : "open";
        item.UpdatesCount = root.TryGetProperty("_nUpdatesCount", out var updates) ? updates.GetInt32() : 0;
        item.HasUpdates = root.TryGetProperty("_bHasUpdates", out var hasUpd) && hasUpd.GetBoolean();
        item.AllTodosCount = root.TryGetProperty("_nAllTodosCount", out var todos) ? todos.GetInt32() : 0;
        item.HasTodos = root.TryGetProperty("_bHasTodos", out var hasTodos) && hasTodos.GetBoolean();
        item.PostCount = root.TryGetProperty("_nPostCount", out var posts) ? posts.GetInt32() : 0;
        item.Tags = root.TryGetProperty("_aTags", out var tags) && tags.ValueKind == JsonValueKind.Array
            ? tags.EnumerateArray()
                .Select(tagElem => new ModTag
                {
                    Title = tagElem.TryGetProperty("_sTitle", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                    Value = tagElem.TryGetProperty("_sValue", out var value) ? value.GetString() ?? string.Empty : string.Empty
                })
                .Where(tag => !string.IsNullOrEmpty(tag.Title) || !string.IsNullOrEmpty(tag.Value))
                .ToList()
            : [];
        item.ThanksCount = root.TryGetProperty("_nThanksCount", out var thanks) ? thanks.GetInt32() : 0;
        item.InitialVisibility = root.TryGetProperty("_sInitialVisibility", out var vis)
            ? vis.GetString() ?? "show"
            : "show";
        item.PayType = root.TryGetProperty("_sPayType", out var pay) ? pay.GetString() ?? "free" : "free";
        item.AccessorSubscriptionRowId = root.TryGetProperty("_idAccessorSubscriptionRow", out var subRow)
            ? subRow.GetInt32()
            : 0;

        // Text (Long Description) Format
        item.Text = root.TryGetProperty("_sText", out var text) ? "<body>" + text.GetString() + "</body>" : string.Empty;
        
        
        // Files (active, non‑archived)
        if (root.TryGetProperty("_aFiles", out var files))
        {
            foreach (var fileElem in files.EnumerateArray())
                item.Files.Add(ModFile.FromJson(fileElem));
        }

        // Archived files
        if (root.TryGetProperty("_aArchivedFiles", out var archived))
        {
            foreach (var archElem in archived.EnumerateArray())
                item.ArchivedFiles.Add(ModFile.FromJson(archElem));
        }

        // Credits (new structured format)
        if (root.TryGetProperty("_aCredits", out var credits))
        {
            foreach (var groupElem in credits.EnumerateArray())
                item.Credits.Add(ModCreditsGroup.FromJson(groupElem));
        }

        // Requirements
        if (root.TryGetProperty("_aRequirements", out var reqs))
        {
            foreach (var pair in reqs.EnumerateArray().Select(req =>
                         req.EnumerateArray().Select(part => part.GetString() ?? string.Empty).ToList()))
            {
                item.Requirements.Add(pair);
            }
        }

        return item;
    }

    /// <summary>
    /// Tries to load the images established in the stored URLs inside this object.
    /// </summary>
    public async Task AttemptToLoadImagesFromURLs(GamebananaApiService apiService, bool reloadIfAlreadySet,
        IProgress<ProgressReport>? progress, CancellationToken cancellationToken = default)
    {
        if (ValidateString(ThumbnailUrl, ThumbnailUrlAsImage))
            ThumbnailUrlAsImage = (await apiService.GetImageAsync(ThumbnailUrl, progress, cancellationToken)).Value;

        if (ValidateString(ImageUrl, ImageUrlAsImage))
            ImageUrlAsImage = (await apiService.GetImageAsync(ImageUrl, progress, cancellationToken)).Value;
        return;

        bool ValidateString([NotNullWhen(true)] string? url, Bitmap? img) =>
            !string.IsNullOrEmpty(url) && (img == null || reloadIfAlreadySet);
    }

// ===== Nested supporting classes =====

    public class ModSubmitter
    {
        [JsonPropertyName("_sName")] public string Name { get; set; } = string.Empty;

        public static ModSubmitter FromJson(JsonElement el) =>
            new()
            {
                Name = el.TryGetProperty("_sName", out var name) ? name.GetString() ?? "Unknown" : "Unknown"
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
        [JsonPropertyName("_sFile")] public string File { get; set; } = string.Empty;
        [JsonPropertyName("_sFile100")] public string File100 { get; set; } = string.Empty;
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
                File100 =
                    el.TryGetProperty("_sFile100", out var f100) ? f100.GetString() ?? string.Empty : string.Empty,
                File530 = el.TryGetProperty("_sFile530", out var f530) ? f530.GetString() ?? string.Empty : string.Empty
            };
    }

    /// <summary>
    /// Represents a file (either active or archived) inside a mod submission.
    /// </summary>
    public class ModFile
    {
        [JsonPropertyName("_idRow")] public int Id { get; set; }
        [JsonPropertyName("_sFile")] public string FileName { get; set; } = string.Empty;
        [JsonPropertyName("_sVersion")] public string Version { get; set; } = string.Empty;
        [JsonPropertyName("_nFilesize")] public long FileSize { get; set; }
        [JsonPropertyName("_tsDateAdded")] public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("_nDownloadCount")] public int DownloadCount { get; set; }
        [JsonPropertyName("_sDownloadUrl")] public string DownloadUrl { get; set; } = string.Empty;
        [JsonPropertyName("_sMd5Checksum")] public string Md5Checksum { get; set; } = string.Empty;
        [JsonPropertyName("_sAnalysisState")] public string AnalysisState { get; set; } = string.Empty;
        [JsonPropertyName("_sAnalysisResult")] public string AnalysisResult { get; set; } = string.Empty;

        [JsonPropertyName("_sAnalysisResultVerbose")]
        public string AnalysisResultVerbose { get; set; } = string.Empty;

        [JsonPropertyName("_sAvState")] public string AvState { get; set; } = string.Empty;
        [JsonPropertyName("_sAvResult")] public string AvResult { get; set; } = string.Empty;
        [JsonPropertyName("_bIsArchived")] public bool IsArchived { get; set; }
        [JsonPropertyName("_bHasContents")] public bool HasContents { get; set; }

        [JsonPropertyName("_aAnalysisWarnings")]
        public Dictionary<string, List<string>> AnalysisWarnings { get; set; } = new();

        [JsonPropertyName("_aModManagerIntegrations")]
        public List<ModManagerIntegration> ModManagerIntegrations { get; set; } =
            [];

        public static ModFile FromJson(JsonElement el)
        {
            var file = new ModFile
            {
                Id = el.TryGetProperty("_idRow", out var id) ? id.GetInt32() : 0,
                FileName = el.TryGetProperty("_sFile", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                Version = el.TryGetProperty("_sVersion", out var version) ? version.GetString() ?? string.Empty : string.Empty,
                FileSize = el.TryGetProperty("_nFilesize", out var size) ? size.GetInt64() : 0,
                DateAdded = el.TryGetProperty("_tsDateAdded", out var added) ? added.GetUnixDateTime() : default,
                DownloadCount = el.TryGetProperty("_nDownloadCount", out var dl) ? dl.GetInt32() : 0,
                DownloadUrl = el.TryGetProperty("_sDownloadUrl", out var url)
                    ? url.GetString() ?? string.Empty
                    : string.Empty,
                Md5Checksum = el.TryGetProperty("_sMd5Checksum", out var md5)
                    ? md5.GetString() ?? string.Empty
                    : string.Empty,
                AnalysisState = el.TryGetProperty("_sAnalysisState", out var aState)
                    ? aState.GetString() ?? string.Empty
                    : string.Empty,
                AnalysisResult = el.TryGetProperty("_sAnalysisResult", out var aRes)
                    ? aRes.GetString() ?? string.Empty
                    : string.Empty,
                AnalysisResultVerbose = el.TryGetProperty("_sAnalysisResultVerbose", out var aResVerb)
                    ? aResVerb.GetString() ?? string.Empty
                    : string.Empty,
                AvState = el.TryGetProperty("_sAvState", out var avState)
                    ? avState.GetString() ?? string.Empty
                    : string.Empty,
                AvResult =
                    el.TryGetProperty("_sAvResult", out var avRes) ? avRes.GetString() ?? string.Empty : string.Empty,
                IsArchived = el.TryGetProperty("_bIsArchived", out var arch) && arch.GetBoolean(),
                HasContents = el.TryGetProperty("_bHasContents", out var contents) && contents.GetBoolean()
            };

            // Parse analysis warnings
            if (el.TryGetProperty("_aAnalysisWarnings", out var warnings))
            {
                foreach (var prop in warnings.EnumerateObject())
                {
                    var list = prop.Value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToList();
                    file.AnalysisWarnings[prop.Name] = list;
                }
            }

            // Parse mod manager integrations
            if (!el.TryGetProperty("_aModManagerIntegrations", out var integrations)) return file;

            foreach (var integration in integrations.EnumerateArray())
                file.ModManagerIntegrations.Add(ModManagerIntegration.FromJson(integration));

            return file;
        }
        
        [JsonIgnore]
        public IndexedFile? IndexedFile { get; set; }

        public override string ToString() => 
            string.IsNullOrEmpty(Version) 
                ? $"{FileName} ({ByteSize.FromBytes(FileSize).ToString()})" 
                : $"{FileName} ({ByteSize.FromBytes(FileSize).ToString()} | {Version})";
        
        [JsonIgnore]
        public string? FormattedAnalysisWarnings {
            get
            {
                // If there's no warnings, return null
                if (AnalysisWarnings.Count == 0) return null;
                
                // TODO: Add localization here
                StringBuilder strBld = new("This file contains the following warnings:");
                var titleCase = CultureInfo.InvariantCulture.TextInfo.ToTitleCase; // Make "this sentence" become "This Sentence"
                
                // Warn Name.
                foreach (var (warnName, warnList) in AnalysisWarnings)
                {
                    // Return Title.
                    strBld.AppendLine(titleCase(warnName.Replace('_', ' ')));
                    
                    // Return all the list.
                    foreach (var t in warnList)
                        strBld.AppendLine('\t' + "\"" + t + "\"");
                }
                
                return strBld.ToString();
            }
        }
    }

    /// <summary>
    /// Represents a mod manager integration entry inside a file.
    /// </summary>
    public class ModManagerIntegration
    {
        [JsonPropertyName("_idToolRow")] public int ToolId { get; set; }
        [JsonPropertyName("_aGameRowIds")] public List<int> GameIds { get; set; } = [];

        [JsonPropertyName("_sModManagerAlias")]
        public string ModManagerAlias { get; set; } = string.Empty;

        [JsonPropertyName("_sInstallerName")] public string InstallerName { get; set; } = string.Empty;
        [JsonPropertyName("_idSubmitterRow")] public int SubmitterId { get; set; }
        [JsonPropertyName("_sInstallerUrl")] public string InstallerUrl { get; set; } = string.Empty;
        [JsonPropertyName("_sIconUrl")] public string IconUrl { get; set; } = string.Empty;
        [JsonPropertyName("_sDownloadUrl")] public string DownloadUrl { get; set; } = string.Empty;

        public static ModManagerIntegration FromJson(JsonElement el) =>
            new()
            {
                ToolId = el.TryGetProperty("_idToolRow", out var tool) ? tool.GetInt32() : 0,
                GameIds = el.TryGetProperty("_aGameRowIds", out var gameIds)
                    ? gameIds.EnumerateArray().Select(g => g.GetInt32()).ToList()
                    : [],
                ModManagerAlias = el.TryGetProperty("_sModManagerAlias", out var alias)
                    ? alias.GetString() ?? string.Empty
                    : string.Empty,
                InstallerName = el.TryGetProperty("_sInstallerName", out var instName)
                    ? instName.GetString() ?? string.Empty
                    : string.Empty,
                SubmitterId = el.TryGetProperty("_idSubmitterRow", out var sub) ? sub.GetInt32() : 0,
                InstallerUrl = el.TryGetProperty("_sInstallerUrl", out var instUrl)
                    ? instUrl.GetString() ?? string.Empty
                    : string.Empty,
                IconUrl =
                    el.TryGetProperty("_sIconUrl", out var icon) ? icon.GetString() ?? string.Empty : string.Empty,
                DownloadUrl = el.TryGetProperty("_sDownloadUrl", out var dlUrl)
                    ? dlUrl.GetString() ?? string.Empty
                    : string.Empty
            };
    }

    /// <summary>
    /// Represents a credits group (e.g., "Key Authors", "Contributors").
    /// </summary>
    public class ModCreditsGroup
    {
        [JsonPropertyName("_sGroupName")] public string GroupName { get; set; } = string.Empty;
        [JsonPropertyName("_aAuthors")] public List<ModCreditAuthor> Authors { get; set; } = [];

        public static ModCreditsGroup FromJson(JsonElement el)
        {
            var group = new ModCreditsGroup
            {
                GroupName = el.TryGetProperty("_sGroupName", out var name)
                    ? name.GetString() ?? string.Empty
                    : string.Empty
            };

            if (!el.TryGetProperty("_aAuthors", out var authors)) return group;

            foreach (var authElem in authors.EnumerateArray())
                group.Authors.Add(ModCreditAuthor.FromJson(authElem));

            return group;
        }
    }
    
    /// <summary>
    /// Represents the tag inside this mod.
    /// </summary>
    public class ModTag
    {
        [JsonPropertyName("_sTitle")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("_sValue")] public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a single author inside a credits group.
    /// </summary>
    public class ModCreditAuthor
    {
        [JsonPropertyName("_sRole")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("_idRow")] public int Id { get; set; }
        [JsonPropertyName("_sName")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("_sUpicUrl")] public string UpicUrl { get; set; } = string.Empty;
        [JsonPropertyName("_sProfileUrl")] public string ProfileUrl { get; set; } = string.Empty;
        [JsonPropertyName("_sAvatarUrl")] public string AvatarUrl { get; set; } = string.Empty;
        [JsonPropertyName("_bIsOnline")] public bool IsOnline { get; set; }

        [JsonPropertyName("_aAffiliatedStudio")]
        public ModAffiliatedStudio? AffiliatedStudio { get; set; }

        public static ModCreditAuthor FromJson(JsonElement el)
        {
            var author = new ModCreditAuthor
            {
                Role = el.TryGetProperty("_sRole", out var role) ? role.GetString() ?? string.Empty : string.Empty,
                Id = el.TryGetProperty("_idRow", out var id) ? id.GetInt32() : 0,
                Name = el.TryGetProperty("_sName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                UpicUrl =
                    el.TryGetProperty("_sUpicUrl", out var upic) ? upic.GetString() ?? string.Empty : string.Empty,
                ProfileUrl = el.TryGetProperty("_sProfileUrl", out var prof)
                    ? prof.GetString() ?? string.Empty
                    : string.Empty,
                AvatarUrl = el.TryGetProperty("_sAvatarUrl", out var avatar)
                    ? avatar.GetString() ?? string.Empty
                    : string.Empty,
                IsOnline = el.TryGetProperty("_bIsOnline", out var online) && online.GetBoolean()
            };

            if (el.TryGetProperty("_aAffiliatedStudio", out var studio))
                author.AffiliatedStudio = ModAffiliatedStudio.FromJson(studio);

            return author;
        }
    }

    /// <summary>
    /// Represents an affiliated studio (used in credits and submitter).
    /// </summary>
    public class ModAffiliatedStudio
    {
        [JsonPropertyName("_sProfileUrl")] public string ProfileUrl { get; set; } = string.Empty;
        [JsonPropertyName("_sName")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("_sFlagUrl")] public string FlagUrl { get; set; } = string.Empty;
        [JsonPropertyName("_sBannerUrl")] public string BannerUrl { get; set; } = string.Empty;

        public static ModAffiliatedStudio FromJson(JsonElement el) =>
            new()
            {
                ProfileUrl = el.TryGetProperty("_sProfileUrl", out var prof)
                    ? prof.GetString() ?? string.Empty
                    : string.Empty,
                Name = el.TryGetProperty("_sName", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                FlagUrl =
                    el.TryGetProperty("_sFlagUrl", out var flag) ? flag.GetString() ?? string.Empty : string.Empty,
                BannerUrl = el.TryGetProperty("_sBannerUrl", out var banner)
                    ? banner.GetString() ?? string.Empty
                    : string.Empty
            };
    }
}