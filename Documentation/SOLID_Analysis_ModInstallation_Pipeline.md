# SOLID Principles Analysis Report: Mod Installation Pipeline

**Date:** June 2026  
**Scope:** Web-to-Local Mod Installation Pipeline  
**Entry Point:** `ModSelectionDialogViewModel.cs`  
**Target Services:** `GamebananaApiService`, `ModInstaller`, `ResourceInstaller`, `ProfileManager`

---

## Executive Summary

This report provides a comprehensive SOLID principles analysis of the mod installation pipeline, starting from the UI layer (`ModSelectionDialogViewModel.cs`) through API consumption (`GamebananaApiService`), local installation (`ModInstaller`), and profile registration (`ProfileManager`). 

The analysis identifies critical violations across all five SOLID principles and proposes a refactored architecture that:
- Abstracts mod source providers (GameBanana, future GitHub API) behind unified interfaces
- Separates concerns between orchestration, downloading, and local installation
- Introduces extensibility patterns for new asset types and sorting strategies
- Enables testability through interface-based dependencies

---

## Table of Contents

1. [Current Pipeline Architecture](#1-current-pipeline-architecture)
2. [SOLID Violations Analysis](#2-solid-violations-analysis)
3. [Proposed Refactored Architecture](#3-proposed-refactored-architecture)
4. [New Service Definitions](#4-new-service-definitions)
5. [Implementation Examples](#5-implementation-examples)
6. [Flow Maps](#6-flow-maps)
7. [Migration Strategy](#7-migration-strategy)
8. [Testing Strategy](#8-testing-strategy)
9. [Additional Architectural Concerns](#9-additional-architectural-concerns)

---

## 1. Current Pipeline Architecture

### 1.1 Component Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    ModSelectionDialogViewModel                  │
│  (778 lines - UI State, API Coordination, Download Orchestration)│
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
        ┌────────────────────────────────────────┐
        │         GamebananaApiService           │
        │  - GetSubmissionDataAsync()            │
        │  - GetSubmissionListAsync()            │
        │  - GetIndexedFileFromFileId()          │
        │  - DownloadFile() ← MIXED CONCERNS     │
        │  - GetImageAsync() ← MIXED CONCERNS    │
        └────────────────────────────────────────┘
                              │
                              ▼
        ┌────────────────────────────────────────┐
        │              ModInstaller              │
        │  - InstallModArchiveAsync()            │
        │  Dependencies:                         │
        │    • ModArchiveExtractor               │
        │    • ManifestLoader                    │
        │    • SecurityScanner                   │
        │    • ResourceInstaller                 │
        │    • ProfileManager ← WRONG LAYER      │
        │    • GameEnvironmentController         │
        │    • SettingsService                   │
        └────────────────────────────────────────┘
                              │
                              ▼
        ┌────────────────────────────────────────┐
        │           ResourceInstaller            │
        │  - InstallResources()                  │
        │  Hardcoded switch on AssetType:        │
        │    • Asset → Directory.Move()          │
        │    • Plugin → File.Move()              │
        │    • Patcher → File.Move()             │
        └────────────────────────────────────────┘
                              │
                              ▼
        ┌────────────────────────────────────────┐
        │             ProfileManager             │
        │  - SetActiveProfile()                  │
        │  - SaveActiveProfile()                 │
        │  - UpdateProfileRepository()           │
        └────────────────────────────────────────┘
```

### 1.2 Current Data Flow

```
User Clicks "Install" 
       │
       ▼
ModSelectionDialogViewModel.InstallPromptAsync()
       │
       ├─► Creates LoadingDialogViewModel[] via CreateInstallationTasks()
       │       │
       │       └─► Each task executes:
       │               1. CreateTempSubdirectory()
       │               2. GamebananaApiService.DownloadFile()
       │               3. ModInstaller.InstallModArchiveAsync()
       │                       │
       │                       ├─► ModArchiveExtractor.ExtractToTempAsync()
       │                       ├─► ManifestLoader.LoadMetadataAsync()
       │                       ├─► SecurityScanner.ScanAsync()
       │                       ├─► ResourceInstaller.InstallResources()
       │                       └─► ProfileManager.ActiveProfile.ModDataFiles.Add() ← VIOLATION
       │
       └─► Shows MultiLoadingDialog with progress
```

---

## 2. SOLID Violations Analysis

### 2.1 Single Responsibility Principle (SRP) Violations

#### Violation 2.1.1: `ModSelectionDialogViewModel.cs` (778 lines)

**Problem:** The ViewModel handles too many responsibilities:
- UI state management (SelectedMod, SelectedFile, EnqueuedModsToInstall)
- API coordination (calling `GamebananaApiService`)
- Download orchestration (creating temp directories, managing download progress)
- Installation task creation (`CreateInstallationTasks`)
- Dialog lifecycle management
- Search and pagination logic
- Sorting/filtering logic

**Evidence:**
```csharp
// Line 556-625: CreateInstallationTasks method
private LoadingDialogViewModel[] CreateInstallationTasks(
    Dictionary<ModItem, ModItem.ModFile> modsToInstall,
    LogContainer logContainer)
{
    foreach (var (mod, file) in modsToInstall)
    {
        var loadingVm = _dialogService.GetDialog<LoadingDialogViewModel>();
        loadingVm.Prepare(
            $"Installing {mod.Name}",
            "Downloading and installing...",
            new Func<IProgress<ProgressReport>, CancellationToken, Task<bool>>(
                async (progress, ct) =>
                {
                    // Creates temp directory
                    using var tempDir = _gameEnvironmentController.CreateTempSubdirectory(Log.Logger);
                    
                    // Downloads file (orchestration logic)
                    var downloadResult = await _gamebananaApiService.DownloadFile(
                        file, tempDir.DirectoryInfo.FullName, _gameEnvironmentController, 
                        progress, Log.Logger, ct);
                    
                    // Installs mod (should be delegated)
                    var installResult = await _modInstaller.InstallModArchiveAsync(
                        downloadResult.Value, progress, ct);
                    
                    // Logs results
                    if (installResult.SecurityIssues.Count != 0)
                        foreach (var issue in installResult.SecurityIssues)
                            logContainer.AddWarning(issue);
                    
                    return true;
                })));
    }
}
```

**Impact:**
- Difficult to test individual concerns
- Changes to download logic require ViewModel changes
- Changes to installation logic require ViewModel changes
- Cannot reuse download/install orchestration elsewhere

#### Violation 2.1.2: `GamebananaApiService.cs` - Mixed API Retrieval and Download Concerns

**Problem:** The service mixes API data retrieval with HTTP content downloads. The TODO comment at line 11 acknowledges this:

```csharp
// TODO: Split this class into two: GamebananaApiRetriever (for retrieving data from API only) 
// and GamebananaApiDownloader (for Image and File downloads).
public class GamebananaApiService(IHttpClientFactory httpClientFactory)
{
    // API Data Methods
    public async Task<Result<ModItem>> GetSubmissionDataAsync(int id) { ... }
    public async Task<Result<GameBananaIndex>> GetSubmissionListAsync(int page, string? searchTerm) { ... }
    public async Task<Result<IndexedFile>> GetIndexedFileFromFileId(int id) { ... }
    
    // Download Methods - DIFFERENT RESPONSIBILITY
    public async Task<Result<string>> DownloadFile(ModItem.ModFile file, ...) { ... }
    public async Task<Result<Bitmap>> GetImageAsync(string uri, ...) { ... }
}
```

**Impact:**
- Clients needing only data retrieval depend on download capabilities
- Cannot mock download behavior independently
- Violates Interface Segregation (see 2.3)
- Makes unit testing difficult (need to mock HttpClient for simple data operations)

#### Violation 2.1.3: `ModInstaller.cs` Knows About Profile Registration

**Problem:** `ModInstaller` directly manipulates `ProfileManager`, which is outside its responsibility scope:

```csharp
// ModInstaller.cs - Line 19: Dependency injection includes ProfileManager
public sealed class ModInstaller(
    ILogger logger,
    ModArchiveExtractor modArchiveExtractor,
    ManifestLoader manifestLoader,
    SecurityScanner securityScanner,
    ResourceInstaller resourceInstaller,
    ProfileManager profileManager,  // ← WRONG: Installer shouldn't know about profiles
    GameEnvironmentController controller,
    SettingsService settingsService)
{
    // Line 104-105: Directly modifies profile
    public async Task<ModInstallationResult> InstallModArchiveAsync(...)
    {
        // ... extraction, scanning, resource installation ...
        
        // 6. Register the mod to the available profile.
        var currentProfile = _profileManager.ActiveProfile;
        currentProfile?.ModDataFiles.Add(manifest);  // ← SRP VIOLATION
        
        results.Metadata = manifest;
        results.Success = true;
    }
}
```

**Impact:**
- `ModInstaller` cannot be used without an active profile
- Cannot install mods without automatic registration
- Testing requires mocking entire profile system
- Prevents scenarios like "install but don't activate" or "install to staging area"

#### Violation 2.1.4: `ModManifestUtils.cs` Uses Static State

**Problem:** Utility class with static methods makes testing difficult and violates SRP by combining multiple unrelated operations:

```csharp
// Found via codebase search
public static class ModManifestUtils
{
    // Combines path calculation, resource enumeration, type conversion
    public static string GetPluginDirectoryFromManifest(...) { ... }
    public static IEnumerable<(AssetType, Resource)> GetAllResources(...) { ... }
    // ... many more static methods
}
```

**Impact:**
- Cannot mock static methods
- Global state makes tests non-isolated
- Difficult to extend behavior

### 2.2 Open/Closed Principle (OCP) Violations

#### Violation 2.2.1: `ResourceInstaller.cs` Hardcoded Switch Statement

**Problem:** Adding new asset types requires modifying the existing class:

```csharp
// ResourceInstaller.cs - Lines 47-97
foreach (var (assetType, resource) in resources)
{
    switch (assetType)
    {
        case ModManifestUtils.AssetType.Asset:
            // Directory move logic
            Directory.Move(resource.LocalPath, resource.MovedAsset);
            break;
        case ModManifestUtils.AssetType.Plugin:
            // Plugin move logic
            File.Move(resource.LocalPath, pluginDestinationPath, true);
            break;
        case ModManifestUtils.AssetType.Patcher:
            // Patcher move logic
            File.Move(resource.LocalPath, patcherDestinationPath, true);
            break;
        default:
            throw new InvalidOperationException("Invalid AssetType value.");
    }
}
```

**Impact:**
- Adding a new asset type (e.g., `Shader`, `AudioPack`) requires:
  1. Modifying `ModManifestUtils.AssetType` enum
  2. Modifying `ResourceInstaller.InstallResources()` switch statement
  3. Risk of breaking existing functionality
- Cannot add custom asset handlers without recompilation
- Violates "closed for modification"

#### Violation 2.2.2: `FilterTypes` Enum Requires Modification for New Sort Options

**Problem:** The enum in `ModSelectionDialogViewModel.cs` must be modified for each new sort option:

```csharp
// ModSelectionDialogViewModel.cs - Lines 30-47
public enum FilterTypes
{
    None,
    NameAscending,
    NameDescending,
    AuthorAscending,
    AuthorDescending,
    DateAddedAscending,
    DateAddedDescending,
    DateModifiedAscending,
    DateModifiedDescending,
    DateUpdatedAscending,
    DateUpdatedDescending,
    DownloadCountAscending,
    DownloadCountDescending,
    ViewCountAscending,
    ViewCountDescending
}

// Lines 505-536: Massive switch expression that grows with each enum value
private void ApplyFilterAndUpdateDisplay(FilterTypes filterType)
{
    Mods = new ObservableCollection<ModItem>(filterType switch
    {
        FilterTypes.NameAscending => AllMods.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase),
        FilterTypes.NameDescending => AllMods.OrderByDescending(m => m.Name, StringComparer.OrdinalIgnoreCase),
        // ... 12 more cases ...
    });
}
```

**Impact:**
- Adding composite sorts (e.g., "Name then Date") requires enum modification
- Cannot add runtime-defined sort criteria
- Switch statement grows linearly with enum values

### 2.3 Interface Segregation Principle (ISP) Violations

#### Violation 2.3.1: `GamebananaApiService` is a "Fat Interface"

**Problem:** Clients that only need data retrieval are forced to depend on download methods:

```csharp
// A hypothetical client that only needs data:
public class ModSearchService
{
    private readonly GamebananaApiService _api; // ← Forced to depend on download methods
    
    public async Task<ModItem> FindModById(int id)
    {
        // Only uses GetSubmissionDataAsync, but depends on DownloadFile and GetImageAsync
        var result = await _api.GetSubmissionDataAsync(id);
        return result.Value;
    }
}
```

**Impact:**
- Unnecessary dependencies
- Harder to mock (must implement/download methods even if unused)
- Prevents independent evolution of retrieval vs. download concerns

#### Violation 2.3.2: `ModInstaller` Has Too Many Dependencies

**Problem:** The constructor has 8 dependencies, some unrelated to core installation:

```csharp
public sealed class ModInstaller(
    ILogger logger,                    // ✓ Relevant
    ModArchiveExtractor modArchiveExtractor,  // ✓ Relevant
    ManifestLoader manifestLoader,     // ✓ Relevant
    SecurityScanner securityScanner,   // ✓ Relevant
    ResourceInstaller resourceInstaller, // ✓ Relevant
    ProfileManager profileManager,     // ✗ NOT relevant to installation
    GameEnvironmentController controller, // ? Borderline (could be abstracted)
    SettingsService settingsService)   // ? Could be configuration object
```

**Impact:**
- Difficult to construct manually
- Test setup is complex
- Indicates the class does too much

### 2.4 Dependency Inversion Principle (DIP) Violations

#### Violation 2.4.1: All Services Depend on Concrete Classes

**Problem:** No interfaces exist for key services:

```csharp
// ModSelectionDialogViewModel.cs - Lines 53-56
private DialogService _dialogService = null!;
private GamebananaApiService _gamebananaApiService = null!;  // ← Concrete class
private GameEnvironmentController _gameEnvironmentController = null!;  // ← Concrete class
private ModInstaller _modInstaller = null!;  // ← Concrete class

// ModInstaller.cs - Lines 13-21
public sealed class ModInstaller(
    ILogger logger,
    ModArchiveExtractor modArchiveExtractor,  // ← Concrete class
    ManifestLoader manifestLoader,  // ← Concrete class
    SecurityScanner securityScanner,  // ← Concrete class
    ResourceInstaller resourceInstaller,  // ← Concrete class
    ProfileManager profileManager,  // ← Concrete class
    GameEnvironmentController controller,  // ← Concrete class
    SettingsService settingsService)  // ← Concrete class
```

**Impact:**
- Cannot substitute mock implementations easily
- Cannot swap implementations at runtime
- Tight coupling prevents parallel development
- Future GitHub API integration will require extensive refactoring

#### Violation 2.4.2: `ModManifestUtils` Static Class Prevents Abstraction

**Problem:** Static utility classes cannot be injected or mocked:

```csharp
// Used throughout the codebase
var pluginDir = DirectoryUtils.GetOrCreate(
    manifest.GetPluginDirectoryFromManifest(_gameEnvironmentController));
```

**Impact:**
- Cannot provide alternative path calculation strategies
- Testing requires actual file system access
- Violates dependency inversion (high-level modules depend on static low-level utilities)

### 2.5 Liskov Substitution Principle (LSP) Considerations

#### Issue 2.5.1: Inconsistent Result Pattern Usage

**Problem:** Some methods return `Result<T>`, others return `null` or throw exceptions:

```csharp
// GamebananaApiService.cs - Returns Result<T>
public async Task<Result<ModItem>> GetSubmissionDataAsync(int id)

// ModInstaller.cs - Returns null implicitly via empty result
public async Task<ModInstallationResult> InstallModArchiveAsync(...)
{
    if (string.IsNullOrEmpty(temporaryDirectory))
        return results;  // ← Returns empty result, not Result<T>
    
    if (manifest == null)
        return results;  // ← Inconsistent with Result pattern
}
```

**Impact:**
- Callers must handle multiple error patterns
- Cannot rely on consistent error handling
- Subtypes (if they existed) would struggle to maintain consistency

---

## 3. Proposed Refactored Architecture

### 3.1 Design Goals

1. **Source Agnosticism:** Abstract mod providers (GameBanana, GitHub, Nexus Mods) behind unified interfaces
2. **Separation of Concerns:** Separate orchestration, downloading, and installation
3. **Extensibility:** Use strategy patterns for asset handling and sorting
4. **Testability:** Depend on interfaces, not concrete classes
5. **Clear Naming:** Emphasize local vs. remote operations

### 3.2 High-Level Architecture (4-Layer Approach)

```
┌─────────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                             │
│  ModSelectionDialogViewModel                                        │
│  - UI state only                                                    │
│  - Delegates to IModInstallationOrchestrator                        │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                              │
│  IModInstallationOrchestrator / ModInstallationOrchestrator         │
│  - Coordinates: Retrieve → Download → Install → Register            │
│  - Handles concurrency, progress, cancellation                      │
│  - Source-agnostic (works with any IModClientAccessor)              │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        DOMAIN LAYER                                 │
│  Interfaces:                                                        │
│    IModClientAccessor    - Abstract mod source (GameBanana, GitHub) │
│    IModContentDownloader - HTTP content downloads                   │
│    ILocalModInstaller    - Install archives locally                 │
│    IModRegistrationService - Profile registration                   │
│    IResourceHandler      - Strategy for asset types                 │
│    ISortStrategy         - Strategy for sorting                     │
└─────────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     INFRASTRUCTURE LAYER                            │
│  Implementations:                                                   │
│    GameBananaClientAccessor  - GameBanana API specifics             │
│    GitHubClientAccessor      - GitHub API specifics (future)        │
│    HttpContentDownloader     - HTTP downloads                       │
│    LocalModArchiveInstaller  - Archive extraction & file moves      │
│    ProfileRegistrationService - Profile management                  │
│    AssetResourceHandler      - Handles Asset-type resources         │
│    PluginResourceHandler     - Handles Plugin-type resources        │
│    PatcherResourceHandler    - Handles Patcher-type resources       │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.3 Key Naming Decisions

| Old Name | New Name | Rationale |
|----------|----------|-----------|
| `GamebananaApiService` | `IModClientAccessor` (interface) + `GameBananaClientAccessor` (implementation) | Abstracts mod source; supports future GitHub/Nexus integration |
| `GamebananaApiService.DownloadFile()` | `IModContentDownloader.DownloadAsync()` | Separates data retrieval from content download |
| `ModInstaller` | `ILocalModInstaller` (interface) + `LocalModArchiveInstaller` (implementation) | Emphasizes LOCAL installation; differentiates from orchestrator |
| N/A | `IModInstallationOrchestrator` | New service coordinating full pipeline |
| N/A | `IModRegistrationService` | Extracted from ModInstaller for profile registration |

---

## 4. New Service Definitions

### 4.1 Core Pipeline Services

#### 4.1.1 `IModClientAccessor` (Interface)

**Purpose:** Abstracts interaction with mod hosting platforms (GameBanana, GitHub, etc.)

```csharp
public interface IModClientAccessor
{
    /// <summary>
    /// Gets the platform name (e.g., "GameBanana", "GitHub").
    /// </summary>
    string PlatformName { get; }
    
    /// <summary>
    /// Retrieves mod metadata by platform-specific ID.
    /// </summary>
    Task<Result<ModItem>> GetModByIdAsync(string modId, CancellationToken ct = default);
    
    /// <summary>
    /// Searches for mods with optional filters.
    /// </summary>
    Task<Result<ModSearchResult>> SearchModsAsync(string searchTerm, int page, CancellationToken ct = default);
    
    /// <summary>
    /// Gets file metadata by file ID.
    /// </summary>
    Task<Result<ModFileMetadata>> GetFileMetadataAsync(string fileId, CancellationToken ct = default);
}
```

**Responsibilities:**
- Platform-specific API calls
- Data transformation to domain models (`ModItem`, `ModFileMetadata`)
- Error handling and Result pattern

**NOT Responsible For:**
- Downloading files (delegated to `IModContentDownloader`)
- Installing mods (delegated to `ILocalModInstaller`)

#### 4.1.2 `IModContentDownloader` (Interface)

**Purpose:** Handles HTTP content downloads (files, images) from any URL

```csharp
public interface IModContentDownloader
{
    /// <summary>
    /// Downloads a file from a URL to a specified destination.
    /// </summary>
    Task<Result<DownloadedFile>> DownloadAsync(
        string url,
        string destinationDirectory,
        string? preferredFileName = null,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Downloads an image as a Bitmap.
    /// </summary>
    Task<Result<Bitmap>> DownloadImageAsync(
        string url,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default);
}

public record DownloadedFile(string FilePath, long SizeBytes, string SourceUrl);
```

**Responsibilities:**
- HTTP GET requests with progress reporting
- Stream handling and buffering
- Temporary file management during download
- Moving completed downloads to final destination

#### 4.1.3 `ILocalModInstaller` (Interface)

**Purpose:** Installs mod archives from local file paths to game directories

```csharp
public interface ILocalModInstaller
{
    /// <summary>
    /// Installs a mod archive from a local file path.
    /// Does NOT handle downloading or profile registration.
    /// </summary>
    Task<InstallationResult> InstallAsync(
        string archivePath,
        InstallationOptions options,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default);
}

public record InstallationOptions
{
    public bool ScanForSecurityIssues { get; init; } = true;
    public bool CancelOnSecurityIssues { get; init; } = false;
    public bool ActivateAfterInstall { get; init; } = true;
}

public record InstallationResult
{
    public bool Success { get; init; }
    public ModManifest? InstalledManifest { get; init; }
    public IReadOnlyList<SecurityIssue> SecurityIssues { get; init; } = [];
    public string? ErrorMessage { get; init; }
}
```

**Responsibilities:**
- Archive extraction
- Manifest loading
- Security scanning
- Resource installation (via `IResourceHandler` strategies)
- **NOT** responsible for profile registration

#### 4.1.4 `IModInstallationOrchestrator` (Interface)

**Purpose:** Coordinates the full pipeline: retrieve → download → install → register

```csharp
public interface IModInstallationOrchestrator
{
    /// <summary>
    /// Orchestrates the complete installation of a mod from a remote source.
    /// </summary>
    Task<OrchestrationResult> InstallFromSourceAsync(
        IModClientAccessor source,
        string modId,
        string fileId,
        InstallationOptions options,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Orchestrates batch installation of multiple mods.
    /// </summary>
    Task<IReadOnlyList<OrchestrationResult>> InstallBatchAsync(
        IEnumerable<(IModClientAccessor Source, string ModId, string FileId)> mods,
        InstallationOptions options,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default);
}

public record OrchestrationResult
{
    public bool Success { get; init; }
    public string? ModName { get; init; }
    public InstallationResult? InstallationResult { get; init; }
    public string? ErrorMessage { get; init; }
}
```

**Responsibilities:**
- Coordinating multiple services
- Managing temporary directories
- Progress aggregation
- Error handling and rollback
- Concurrent batch processing

#### 4.1.5 `IModRegistrationService` (Interface)

**Purpose:** Handles profile registration/unregistration of installed mods

```csharp
public interface IModRegistrationService
{
    /// <summary>
    /// Registers an installed mod with the active profile.
    /// </summary>
    Task<Result<Unit>> RegisterModAsync(
        ModManifest manifest,
        string profileName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Unregisters a mod from a profile.
    /// </summary>
    Task<Result<Unit>> UnregisterModAsync(
        string manifestId,
        string profileName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets the currently active profile name.
    /// </summary>
    string? GetActiveProfileName();
}
```

**Responsibilities:**
- Profile manipulation
- Mod data file management
- Activation state tracking

### 4.2 Extensibility Services (Strategy Pattern)

#### 4.2.1 `IResourceHandler` (Interface)

**Purpose:** Strategy interface for handling different resource types

```csharp
public interface IResourceHandler
{
    /// <summary>
    /// The asset type this handler processes.
    /// </summary>
    ResourceType SupportedType { get; }
    
    /// <summary>
    /// Installs a resource from source to destination.
    /// </summary>
    Task<Result<Unit>> HandleAsync(
        ResourceDescriptor resource,
        InstallationContext context,
        CancellationToken ct = default);
}

public enum ResourceType
{
    Asset,      // Directory resources
    Plugin,     // DLL files for BepInEx
    Patcher,    // DLL files for patchers
    Shader,     // Future: shader files
    AudioPack   // Future: audio resources
}

public record ResourceDescriptor
{
    public ResourceType Type { get; init; }
    public string SourcePath { get; init; }
    public string DestinationPath { get; init; }
    public string RelativePath { get; init; }
}

public record InstallationContext
{
    public string ModRootPath { get; init; }
    public string GameRootPath { get; init; }
    public ModManifest Manifest { get; init; }
    public ILogger Logger { get; init; }
}
```

#### 4.2.2 `ISortStrategy` (Interface)

**Purpose:** Strategy interface for mod sorting

```csharp
public interface ISortStrategy
{
    /// <summary>
    /// The display name of this sort strategy.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Applies sorting to a collection of mods.
    /// </summary>
    IEnumerable<ModItem> Sort(IEnumerable<ModItem> mods);
}

// Example implementations:
public sealed class NameSortStrategy : ISortStrategy
{
    private readonly bool _ascending;
    public string DisplayName => _ascending ? "Name (A-Z)" : "Name (Z-A)";
    
    public IEnumerable<ModItem> Sort(IEnumerable<ModItem> mods) =>
        _ascending 
            ? mods.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            : mods.OrderByDescending(m => m.Name, StringComparer.OrdinalIgnoreCase);
}

public sealed class CompositeSortStrategy : ISortStrategy
{
    private readonly ISortStrategy[] _strategies;
    public string DisplayName => "Custom";
    
    public IEnumerable<ModItem> Sort(IEnumerable<ModItem> mods)
    {
        IEnumerable<ModItem> result = mods;
        foreach (var strategy in _strategies.Reverse())
            result = strategy.Sort(result);
        return result;
    }
}
```

### 4.3 Supporting Services

#### 4.3.1 `ITempDirectoryFactory` (Interface)

**Purpose:** Abstracts temporary directory creation for testability

```csharp
public interface ITempDirectoryFactory
{
    /// <summary>
    /// Creates a temporary directory for installation operations.
    /// </summary>
    IDisposableTemporaryDirectory Create(string prefix = "gmp_install_");
}

public interface IDisposableTemporaryDirectory : IDisposable
{
    DirectoryInfo DirectoryInfo { get; }
    string FullPath { get; }
}
```

#### 4.3.2 `IModManifestService` (Interface)

**Purpose:** Replaces static `ModManifestUtils` with injectable service

```csharp
public interface IModManifestService
{
    /// <summary>
    /// Calculates the plugin directory path for a manifest.
    /// </summary>
    string GetPluginDirectoryPath(ModManifest manifest, string gameRoot);
    
    /// <summary>
    /// Enumerates all resources from a manifest.
    /// </summary>
    IEnumerable<(ResourceType Type, ResourceDescriptor Descriptor)> 
        GetResources(ModManifest manifest, string modRootPath, string gameRoot);
    
    /// <summary>
    /// Saves manifest metadata to disk.
    /// </summary>
    Task SaveMetadataAsync(ModManifest manifest, CancellationToken ct = default);
}
```

#### 4.3.3 `IModThumbnailService` (Interface)

**Purpose:** Handles thumbnail loading and caching

```csharp
public interface IModThumbnailService
{
    /// <summary>
    /// Loads or retrieves a cached thumbnail for a mod.
    /// </summary>
    Task<Result<Bitmap>> GetThumbnailAsync(
        ModItem mod,
        ThumbnailSize size = ThumbnailSize.Medium,
        CancellationToken ct = default);
}

public enum ThumbnailSize { Small, Medium, Large }
```

### 4.4 Service Summary Table

| Service | Type | Priority | Replaces | Purpose |
|---------|------|----------|----------|---------|
| `IModClientAccessor` | Interface | High | N/A | Abstract mod source platforms |
| `GameBananaClientAccessor` | Implementation | High | `GamebananaApiService` (data methods) | GameBanana API implementation |
| `GitHubClientAccessor` | Implementation | Medium | N/A | Future GitHub API implementation |
| `IModContentDownloader` | Interface | High | N/A | Abstract HTTP downloads |
| `HttpContentDownloader` | Implementation | High | `GamebananaApiService.DownloadFile/GetImage` | HTTP download implementation |
| `ILocalModInstaller` | Interface | High | `ModInstaller` | Abstract local installation |
| `LocalModArchiveInstaller` | Implementation | High | `ModInstaller` (core logic) | Archive installation implementation |
| `IModInstallationOrchestrator` | Interface | High | N/A (new) | Coordinate full pipeline |
| `ModInstallationOrchestrator` | Implementation | High | N/A (new) | Orchestration implementation |
| `IModRegistrationService` | Interface | High | N/A (extracted) | Profile registration |
| `ProfileRegistrationService` | Implementation | High | `ProfileManager` (subset) | Registration implementation |
| `IResourceHandler` | Interface | Medium | N/A | Strategy for resource types |
| `AssetResourceHandler` | Implementation | Medium | `ResourceInstaller` (Asset case) | Handle Asset-type resources |
| `PluginResourceHandler` | Implementation | Medium | `ResourceInstaller` (Plugin case) | Handle Plugin-type resources |
| `PatcherResourceHandler` | Implementation | Medium | `ResourceInstaller` (Patcher case) | Handle Patcher-type resources |
| `ISortStrategy` | Interface | Medium | N/A | Strategy for sorting |
| `ITempDirectoryFactory` | Interface | Low | N/A | Temp directory abstraction |
| `IModManifestService` | Interface | Low | `ModManifestUtils` | Replace static utilities |

---

## 5. Implementation Examples

### 5.1 `IModClientAccessor` Implementation (GameBanana)

```csharp
public sealed class GameBananaClientAccessor : IModClientAccessor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string ApiVersion = "apiv12";
    
    public string PlatformName => "GameBanana";
    
    public GameBananaClientAccessor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<Result<ModItem>> GetModByIdAsync(string modId, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("GameBanana");
        var response = await client.GetAsync($"{ApiVersion}/Mod/{modId}/ProfilePage", ct);
        
        if (!response.IsSuccessStatusCode)
            return Result<ModItem>.Failure($"API Error: {response.StatusCode}");
        
        var json = await response.Content.ReadAsStringAsync(ct);
        var document = JsonDocument.Parse(json);
        return Result<ModItem>.Success(ModItem.FromJson(document));
    }
    
    public async Task<Result<ModSearchResult>> SearchModsAsync(
        string searchTerm, int page, CancellationToken ct = default)
    {
        var urlToUse = string.IsNullOrWhiteSpace(searchTerm)
            ? $"/Mod/Index?_nPerpage=15&_aFilters[Generic_Category]=4609&_nPage={page}"
            : $"/Mod/Index?_nPerpage=15&_aFilters[Generic_Category]=4609&_aFilters[Generic_Name]=contains,{searchTerm}&_nPage={page}";
        
        var client = _httpClientFactory.CreateClient("GameBanana");
        var response = await client.GetAsync($"{ApiVersion}{urlToUse}", ct);
        
        if (!response.IsSuccessStatusCode)
            return Result<ModSearchResult>.Failure($"API Error: {response.StatusCode}");
        
        var json = await response.Content.ReadAsStringAsync(ct);
        var document = JsonDocument.Parse(json);
        return Result<ModSearchResult>.Success(GameBananaIndex.FromJson(document));
    }
    
    public Task<Result<ModFileMetadata>> GetFileMetadataAsync(string fileId, CancellationToken ct = default)
    {
        // Implementation similar to GetIndexedFileFromFileId
        throw new NotImplementedException();
    }
}
```

### 5.2 `IModContentDownloader` Implementation

```csharp
public sealed class HttpContentDownloader : IModContentDownloader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITempDirectoryFactory _tempDirectoryFactory;
    private const int DefaultBufferSize = 81920;
    
    public HttpContentDownloader(
        IHttpClientFactory httpClientFactory,
        ITempDirectoryFactory tempDirectoryFactory)
    {
        _httpClientFactory = httpClientFactory;
        _tempDirectoryFactory = tempDirectoryFactory;
    }
    
    public async Task<Result<DownloadedFile>> DownloadAsync(
        string url,
        string destinationDirectory,
        string? preferredFileName = null,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(destinationDirectory))
            return Result<DownloadedFile>.Failure("Destination directory does not exist.");
        
        var client = _httpClientFactory.CreateClient("Default");
        
        using var response = await client.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        
        if (!response.IsSuccessStatusCode)
            return Result<DownloadedFile>.Failure($"HTTP Error: {response.StatusCode}");
        
        using var tempDir = _tempDirectoryFactory.Create("download_");
        
        var fileName = preferredFileName ?? Path.GetFileName(new Uri(url).AbsolutePath);
        var tempFilePath = Path.Combine(tempDir.FullPath, fileName);
        var destinationPath = Path.Combine(destinationDirectory, fileName);
        
        try
        {
            await using var fileStream = File.OpenWrite(tempFilePath);
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            
            var buffer = new byte[DefaultBufferSize];
            var totalBytesRead = 0L;
            var contentLength = response.Content.Headers.ContentLength;
            
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalBytesRead += bytesRead;
                
                if (progress != null && contentLength.HasValue)
                {
                    progress.Report(new ProgressReport(
                        totalBytesRead, 
                        contentLength.Value,
                        currentStatus: "Downloading...",
                        usePercentage: true));
                }
            }
            
            File.Move(tempFilePath, destinationPath, overwrite: true);
            
            return Result<DownloadedFile>.Success(new DownloadedFile(
                destinationPath,
                new FileInfo(destinationPath).Length,
                url));
        }
        catch (OperationCanceledException)
        {
            return Result<DownloadedFile>.Failure("Download cancelled.");
        }
        catch (Exception ex)
        {
            return Result<DownloadedFile>.Failure($"Download failed: {ex.Message}");
        }
    }
    
    public async Task<Result<Bitmap>> DownloadImageAsync(
        string url,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        // Similar implementation for image downloads
        throw new NotImplementedException();
    }
}
```

### 5.3 `ILocalModInstaller` Implementation

```csharp
public sealed class LocalModArchiveInstaller : ILocalModInstaller
{
    private readonly ILogger _logger;
    private readonly IModArchiveExtractor _archiveExtractor;
    private readonly IManifestLoader _manifestLoader;
    private readonly ISecurityScanner _securityScanner;
    private readonly IEnumerable<IResourceHandler> _resourceHandlers;
    private readonly IModManifestService _manifestService;
    
    public LocalModArchiveInstaller(
        ILogger logger,
        IModArchiveExtractor archiveExtractor,
        IManifestLoader manifestLoader,
        ISecurityScanner securityScanner,
        IEnumerable<IResourceHandler> resourceHandlers,
        IModManifestService manifestService)
    {
        _logger = logger;
        _archiveExtractor = archiveExtractor;
        _manifestLoader = manifestLoader;
        _securityScanner = securityScanner;
        _resourceHandlers = resourceHandlers;
        _manifestService = manifestService;
    }
    
    public async Task<InstallationResult> InstallAsync(
        string archivePath,
        InstallationOptions options,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        var securityIssues = new List<SecurityIssue>();
        string? temporaryDirectory = null;
        
        try
        {
            _logger.Information("Initiating installation of {ArchiveName}...", 
                Path.GetFileNameWithoutExtension(archivePath));
            
            // Step 1: Extract archive
            temporaryDirectory = await _archiveExtractor.ExtractToTempAsync(archivePath, ct);
            ct.ThrowIfCancellationRequested();
            
            if (string.IsNullOrEmpty(temporaryDirectory))
                return new InstallationResult 
                { 
                    Success = false, 
                    ErrorMessage = "Failed to extract archive." 
                };
            
            // Step 2: Load manifest
            var manifest = await _manifestLoader.LoadMetadataAsync(
                temporaryDirectory, progress, ct);
            ct.ThrowIfCancellationRequested();
            
            if (manifest == null)
                return new InstallationResult 
                { 
                    Success = false, 
                    ErrorMessage = "Failed to load manifest." 
                };
            
            // Step 3: Security scan
            if (options.ScanForSecurityIssues)
            {
                var scanResult = await _securityScanner.ScanAsync(
                    temporaryDirectory, 
                    new ScanContext { Manifest = manifest },
                    progress, 
                    ct);
                
                securityIssues.AddRange(scanResult.Issues);
                
                if (!scanResult.IsSafe && options.CancelOnSecurityIssues)
                    return new InstallationResult
                    {
                        Success = false,
                        SecurityIssues = securityIssues,
                        ErrorMessage = "Security issues detected and CancelOnSecurityIssues is enabled."
                    };
            }
            
            // Step 4: Install resources using strategy pattern
            var context = new InstallationContext
            {
                ModRootPath = temporaryDirectory,
                GameRootPath = _gameEnvironmentController.GameRootPath,
                Manifest = manifest,
                Logger = _logger
            };
            
            var resources = _manifestService.GetResources(manifest, temporaryDirectory, context.GameRootPath);
            
            foreach (var (type, descriptor) in resources)
            {
                var handler = _resourceHandlers.FirstOrDefault(h => h.SupportedType == type);
                if (handler == null)
                {
                    _logger.Warning("No handler found for resource type {ResourceType}", type);
                    continue;
                }
                
                var result = await handler.HandleAsync(descriptor, context, ct);
                if (result.IsFailure)
                {
                    _logger.Error(result.Error, "Failed to install resource {ResourcePath}", descriptor.SourcePath);
                    return new InstallationResult
                    {
                        Success = false,
                        SecurityIssues = securityIssues,
                        ErrorMessage = $"Failed to install resource: {result.Error}"
                    };
                }
            }
            
            // Step 5: Save manifest metadata
            await _manifestService.SaveMetadataAsync(manifest, ct);
            
            return new InstallationResult
            {
                Success = true,
                InstalledManifest = manifest,
                SecurityIssues = securityIssues
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Installation cancelled.");
            return new InstallationResult 
            { 
                Success = false, 
                ErrorMessage = "Installation cancelled by user." 
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Installation failed.");
            return new InstallationResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message,
                SecurityIssues = securityIssues
            };
        }
        finally
        {
            if (!string.IsNullOrEmpty(temporaryDirectory) && Directory.Exists(temporaryDirectory))
            {
                try { Directory.Delete(temporaryDirectory, true); }
                catch { /* Suppressed */ }
            }
        }
    }
}
```

### 5.4 `IModInstallationOrchestrator` Implementation

```csharp
public sealed class ModInstallationOrchestrator : IModInstallationOrchestrator
{
    private readonly IModContentDownloader _downloader;
    private readonly ILocalModInstaller _installer;
    private readonly IModRegistrationService _registrationService;
    private readonly ITempDirectoryFactory _tempDirectoryFactory;
    private readonly ILogger _logger;
    
    public ModInstallationOrchestrator(
        IModContentDownloader downloader,
        ILocalModInstaller installer,
        IModRegistrationService registrationService,
        ITempDirectoryFactory tempDirectoryFactory,
        ILogger logger)
    {
        _downloader = downloader;
        _installer = installer;
        _registrationService = registrationService;
        _tempDirectoryFactory = tempDirectoryFactory;
        _logger = logger;
    }
    
    public async Task<OrchestrationResult> InstallFromSourceAsync(
        IModClientAccessor source,
        string modId,
        string fileId,
        InstallationOptions options,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            // Step 1: Get file metadata
            var fileResult = await source.GetFileMetadataAsync(fileId, ct);
            if (fileResult.IsFailure)
                return new OrchestrationResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Failed to get file metadata: {fileResult.Error}" 
                };
            
            var fileMetadata = fileResult.Value!;
            
            // Step 2: Create temp directory for download
            using var downloadDir = _tempDirectoryFactory.Create("download_");
            
            // Step 3: Download file
            var downloadProgress = new Progress<ProgressReport>(report =>
                progress?.Report(report.WithPrefix($"[{fileMetadata.Name}] Downloading")));
            
            var downloadResult = await _downloader.DownloadAsync(
                fileMetadata.DownloadUrl,
                downloadDir.FullPath,
                fileMetadata.FileName,
                downloadProgress,
                ct);
            
            if (downloadResult.IsFailure)
                return new OrchestrationResult 
                { 
                    Success = false, 
                    ModName = fileMetadata.Name,
                    ErrorMessage = $"Download failed: {downloadResult.Error}" 
                };
            
            // Step 4: Install from local archive
            var installProgress = new Progress<ProgressReport>(report =>
                progress?.Report(report.WithPrefix($"[{fileMetadata.Name}] Installing")));
            
            var installationResult = await _installer.InstallAsync(
                downloadResult.Value!.FilePath,
                options,
                installProgress,
                ct);
            
            if (!installationResult.Success)
                return new OrchestrationResult 
                { 
                    Success = false, 
                    ModName = fileMetadata.Name,
                    InstallationResult = installationResult,
                    ErrorMessage = installationResult.ErrorMessage 
                };
            
            // Step 5: Register with profile (if configured)
            if (options.ActivateAfterInstall && installationResult.InstalledManifest != null)
            {
                var activeProfile = _registrationService.GetActiveProfileName();
                if (!string.IsNullOrEmpty(activeProfile))
                {
                    var registerResult = await _registrationService.RegisterModAsync(
                        installationResult.InstalledManifest,
                        activeProfile,
                        ct);
                    
                    if (registerResult.IsFailure)
                    {
                        _logger.Warning(registerResult.Error, 
                            "Mod installed but registration failed: {ModName}", fileMetadata.Name);
                    }
                }
            }
            
            return new OrchestrationResult
            {
                Success = true,
                ModName = fileMetadata.Name,
                InstallationResult = installationResult
            };
        }
        catch (OperationCanceledException)
        {
            return new OrchestrationResult 
            { 
                Success = false, 
                ErrorMessage = "Installation cancelled by user." 
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Orchestration failed.");
            return new OrchestrationResult 
            { 
                Success = false, 
                ErrorMessage = ex.Message 
            };
        }
    }
    
    public async Task<IReadOnlyList<OrchestrationResult>> InstallBatchAsync(
        IEnumerable<(IModClientAccessor Source, string ModId, string FileId)> mods,
        InstallationOptions options,
        IProgress<ProgressReport>? progress = null,
        CancellationToken ct = default)
    {
        var tasks = mods.Select(async (item, index) =>
        {
            var batchProgress = new Progress<ProgressReport>(report =>
                progress?.Report(report.WithPrefix($"[{index + 1}/{mods.Count()}] {report.CurrentStatus}")));
            
            return await InstallFromSourceAsync(
                item.Source,
                item.ModId,
                item.FileId,
                options,
                batchProgress,
                ct);
        });
        
        return await Task.WhenAll(tasks);
    }
}
```

### 5.5 Refactored `ModSelectionDialogViewModel`

```csharp
public partial class ModSelectionDialogViewModel : DialogViewModel
{
    private readonly IModInstallationOrchestrator _orchestrator;
    private readonly IDialogService _dialogService;
    
    // Removed: GamebananaApiService, ModInstaller, GameEnvironmentController
    
    public ModSelectionDialogViewModel(
        IModInstallationOrchestrator orchestrator,
        IDialogService dialogService)
    {
        _orchestrator = orchestrator;
        _dialogService = dialogService;
    }
    
    [RelayCommand]
    public async Task InstallPromptAsync()
    {
        if (EnqueuedModsToInstall.Count == 0) return;
        
        Close();
        
        // Preview files
        var previewLogContainer = new LogContainer();
        foreach (var (mod, file) in EnqueuedModsToInstall)
            previewLogContainer.AddInformation(mod.ToString(), file.ToString());
        
        if (await _dialogService.PromptUserQuestion(
                "Confirm Installation",
                $"You are about to install {EnqueuedModsToInstall.Count} mod(s). Proceed?",
                DialogServiceUtils.QuestionAnswerType.ProceedOrCancel,
                previewLogContainer))
        {
            var installationLogContainer = new LogContainer();
            
            // Prepare batch installation parameters
            var modsToInstall = EnqueuedModsToInstall
                .Select(kvp => (
                    Source: GetAccessorForMod(kvp.Key), // Factory method to get right accessor
                    ModId: kvp.Key.Id.ToString(),
                    FileId: kvp.Value.Id.ToString()
                ));
            
            // Use orchestrator for batch installation
            var results = await _orchestrator.InstallBatchAsync(
                modsToInstall,
                new InstallationOptions 
                { 
                    ScanForSecurityIssues = true,
                    CancelOnSecurityIssues = _settingsService.CurrentSettings.CancelOnSecurityIssues
                },
                progress: null, // Handled by orchestrator
                ct: CancellationToken.None);
            
            // Handle results
            if (results.Any(r => !r.Success))
            {
                foreach (var failed in results.Where(r => !r.Success))
                    installationLogContainer.AddError(
                        $"Installation failed for {failed.ModName}",
                        failed.ErrorMessage);
                
                await _dialogService.NotifyUser(
                    "Installation Completed with Issues",
                    "Some mods failed to install. Check the logs for details.",
                    confirmationButton: "View Logs",
                    container: installationLogContainer);
            }
        }
    }
    
    private IModClientAccessor GetAccessorForMod(ModItem mod)
    {
        // Factory method to determine correct accessor based on mod source
        // For now, all mods are from GameBanana
        return _serviceProvider.GetRequiredService<GameBananaClientAccessor>();
    }
}
```

### 5.6 Resource Handler Implementations

```csharp
public sealed class AssetResourceHandler : IResourceHandler
{
    public ResourceType SupportedType => ResourceType.Asset;
    
    public async Task<Result<Unit>> HandleAsync(
        ResourceDescriptor resource,
        InstallationContext context,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(resource.SourcePath))
            return Result<Unit>.Failure($"Source directory does not exist: {resource.SourcePath}");
        
        if (!Directory.Exists(resource.DestinationPath))
            Directory.CreateDirectory(resource.DestinationPath);
        
        context.Logger.Information("Moving asset {Source} to {Destination}", 
            resource.SourcePath, resource.DestinationPath);
        
        Directory.Move(resource.SourcePath, resource.DestinationPath);
        
        return Result<Unit>.Success(Unit.Value);
    }
}

public sealed class PluginResourceHandler : IResourceHandler
{
    private readonly IModManifestService _manifestService;
    
    public ResourceType SupportedType => ResourceType.Plugin;
    
    public PluginResourceHandler(IModManifestService manifestService)
    {
        _manifestService = manifestService;
    }
    
    public async Task<Result<Unit>> HandleAsync(
        ResourceDescriptor resource,
        InstallationContext context,
        CancellationToken ct = default)
    {
        if (!File.Exists(resource.SourcePath))
            return Result<Unit>.Failure($"Plugin file does not exist: {resource.SourcePath}");
        
        var pluginDir = _manifestService.GetPluginDirectoryPath(context.Manifest, context.GameRootPath);
        var destinationPath = Path.Combine(pluginDir, Path.GetFileName(resource.SourcePath));
        
        Directory.CreateDirectory(pluginDir);
        
        context.Logger.Information("Moving plugin {Source} to {Destination}", 
            resource.SourcePath, destinationPath);
        
        File.Move(resource.SourcePath, destinationPath, overwrite: true);
        
        return Result<Unit>.Success(Unit.Value);
    }
}

public sealed class PatcherResourceHandler : IResourceHandler
{
    public ResourceType SupportedType => ResourceType.Patcher;
    
    public async Task<Result<Unit>> HandleAsync(
        ResourceDescriptor resource,
        InstallationContext context,
        CancellationToken ct = default)
    {
        if (!File.Exists(resource.SourcePath))
            return Result<Unit>.Failure($"Patcher file does not exist: {resource.SourcePath}");
        
        var patcherDir = Path.Combine(context.GameRootPath, "BepInEx", "patchers");
        var destinationPath = Path.Combine(patcherDir, Path.GetFileName(resource.SourcePath));
        
        Directory.CreateDirectory(patcherDir);
        
        context.Logger.Information("Moving patcher {Source} to {Destination}", 
            resource.SourcePath, destinationPath);
        
        File.Move(resource.SourcePath, destinationPath, overwrite: true);
        
        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

## 6. Flow Maps

### 6.1 Current Pipeline Flow (Before Refactoring)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         CURRENT PIPELINE FLOW                            │
└──────────────────────────────────────────────────────────────────────────┘

User clicks "Install" in ModSelectionDialog
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ ModSelectionDialogViewModel.InstallPromptAsync()                │
│  - Shows confirmation dialog                                    │
│  - Calls CreateInstallationTasks()                              │
└─────────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ CreateInstallationTasks()                                       │
│  - Creates LoadingDialogViewModel[]                             │
│  - Each VM wraps inline async delegate                          │
└─────────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Per-Task Execution (inline in ViewModel)                        │
│  1. _gameEnvironmentController.CreateTempSubdirectory()         │
│  2. _gamebananaApiService.DownloadFile()                        │
│     - Creates ANOTHER temp directory (nested!)                  │
│     - Downloads via HttpClient                                  │
│     - Moves to destination                                      │
│  3. _modInstaller.InstallModArchiveAsync()                      │
│     - Extracts archive                                          │
│     - Loads manifest                                            │
│     - Scans for security                                        │
│     - Installs resources (hardcoded switch)                     │
│     - DIRECTLY modifies ProfileManager.ActiveProfile ⚠️         │
└─────────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ MultiLoadingDialog shows progress                               │
│  - Aggregates boolean results                                   │
│  - Shows log container on failures                              │
└─────────────────────────────────────────────────────────────────┘

PROBLEMS:
✗ ViewModel knows about download, install, and profile logic
✗ Nested temp directory creation (inefficient)
✗ ModInstaller directly modifies ProfileManager (wrong layer)
✗ ResourceInstaller uses hardcoded switch (not extensible)
✗ No abstraction for mod source (can't add GitHub easily)
✗ All dependencies are concrete classes (hard to test)
```

### 6.2 Proposed Pipeline Flow (After Refactoring)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                      PROPOSED PIPELINE FLOW                              │
└──────────────────────────────────────────────────────────────────────────┘

User clicks "Install" in ModSelectionDialog
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ ModSelectionDialogViewModel.InstallPromptAsync()                │
│  - Shows confirmation dialog                                    │
│  - Prepares batch parameters:                                   │
│    [(IModClientAccessor, ModId, FileId), ...]                   │
│  - Calls _orchestrator.InstallBatchAsync()                      │
│  - Displays results                                             │
└─────────────────────────────────────────────────────────────────┘
              │
              │ Delegates to Orchestrator
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ ModInstallationOrchestrator.InstallBatchAsync()                 │
│  - Spawns concurrent tasks for each mod                         │
│  - Aggregates progress reports                                  │
│  - Returns OrchestrationResult[]                                │
└─────────────────────────────────────────────────────────────────┘
              │
              │ For each mod, executes:
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ ModInstallationOrchestrator.InstallFromSourceAsync()            │
│                                                                 │
│  STEP 1: Get File Metadata                                      │
│    ┌──────────────────────────────────────────────────────┐    │
│    │ source.GetFileMetadataAsync(fileId)                  │    │
│    │ Returns: ModFileMetadata (URL, FileName, Size)       │    │
│    └──────────────────────────────────────────────────────┘    │
│                                                                 │
│  STEP 2: Download File                                          │
│    ┌──────────────────────────────────────────────────────┐    │
│    │ _tempDirectoryFactory.Create("download_")            │    │
│    │ _downloader.DownloadAsync(url, tempDir, fileName)    │    │
│    │ Returns: DownloadedFile (FilePath, Size, Url)        │    │
│    └──────────────────────────────────────────────────────┘    │
│                                                                 │
│  STEP 3: Install Locally                                        │
│    ┌──────────────────────────────────────────────────────┐    │
│    │ _installer.InstallAsync(archivePath, options)        │    │
│    │   ├─ Extract archive                                 │    │
│    │   ├─ Load manifest                                   │    │
│    │   ├─ Security scan                                   │    │
│    │   └─ Install resources (strategy pattern)            │    │
│    │       • AssetResourceHandler                         │    │
│    │       • PluginResourceHandler                        │    │
│    │       • PatcherResourceHandler                       │    │
│    │ Returns: InstallationResult                          │    │
│    └──────────────────────────────────────────────────────┘    │
│                                                                 │
│  STEP 4: Register with Profile (optional)                       │
│    ┌──────────────────────────────────────────────────────┐    │
│    │ if (options.ActivateAfterInstall)                    │    │
│    │   _registrationService.RegisterModAsync(manifest)    │    │
│    └──────────────────────────────────────────────────────┘    │
│                                                                 │
│  Returns: OrchestrationResult                                   │
└─────────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ Results aggregated and displayed to user                        │
│  - Success count                                                │
│  - Failure details in log container                             │
│  - Security warnings highlighted                                │
└─────────────────────────────────────────────────────────────────┘

IMPROVEMENTS:
✓ ViewModel only coordinates UI and delegates to orchestrator
✓ Single temp directory per mod (no nesting)
✓ Orchestrator handles registration, not installer
✓ Resource handlers use strategy pattern (extensible)
✓ IModClientAccessor abstracts mod source (easy to add GitHub)
✓ All dependencies are interfaces (easy to test)
```

### 6.3 Detailed Per-Task Concurrent Execution

```
┌──────────────────────────────────────────────────────────────────────────┐
│              CONCURRENT BATCH INSTALLATION (Detailed)                    │
└──────────────────────────────────────────────────────────────────────────┘

ModInstallationOrchestrator.InstallBatchAsync([
  (GameBanana, "600001", "1001"),
  (GameBanana, "600002", "1002"),
  (GameBanana, "600003", "1003")
])
         │
         ├────────────────────────────────────────────────────┐
         │                                                    │
         ▼                                                    ▼
┌─────────────────────┐                              ┌─────────────────────┐
│ Task 1: Mod 600001  │                              │ Task 2: Mod 600002  │
│                     │                              │                     │
│ 1. Get Metadata     │                              │ 1. Get Metadata     │
│    └─► GB API       │                              │    └─► GB API       │
│                     │                              │                     │
│ 2. Download         │                              │ 2. Download         │
│    └─► HTTP         │                              │    └─► HTTP         │
│                     │                              │                     │
│ 3. Install          │                              │ 3. Install          │
│    ├─► Extract      │                              │    ├─► Extract      │
│    ├─► Manifest     │                              │    ├─► Manifest     │
│    ├─► Scan         │                              │    ├─► Scan         │
│    └─► Resources    │                              │    └─► Resources    │
│       (strategies)  │                              │       (strategies)  │
│                     │                              │                     │
│ 4. Register         │                              │ 4. Register         │
│    └─► Profile      │                              │    └─► Profile      │
│                     │                              │                     │
│ Result: Success ✓   │                              │ Result: Success ✓   │
└─────────────────────┘                              └─────────────────────┘
         │                                                    │
         │                                                    │
         ▼                                                    ▼
┌─────────────────────┐                              ┌─────────────────────┐
│ Task 3: Mod 600003  │                              │ ...more tasks...    │
│                     │                              │                     │
│ 1. Get Metadata     │                              │                     │
│    └─► GB API       │                              │                     │
│                     │                              │                     │
│ 2. Download         │                              │                     │
│    └─► FAILED       │                              │                     │
│       (404 Error)   │                              │                     │
│                     │                              │                     │
│ Result: Failed ✗    │                              │                     │
│ Error: "HTTP 404"   │                              │                     │
└─────────────────────┘                              └─────────────────────┘
         │
         │
         ▼
┌─────────────────────────────────────────────────────────────────┐
│ Task.WhenAll() completes                                        │
│ Returns: OrchestrationResult[]                                  │
│   [                                         │
│     { Success=true, ModName="Mod1" },       │
│     { Success=true, ModName="Mod2" },       │
│     { Success=false, Error="HTTP 404" }     │
│   ]                                         │
└─────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────┐
│ ViewModel displays results                                      │
│  - "2 of 3 mods installed successfully"                         │
│  - Shows log container with failure details                     │
└─────────────────────────────────────────────────────────────────┘
```

### 6.4 Strategy Pattern for Resource Handlers

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    RESOURCE HANDLER STRATEGY PATTERN                     │
└──────────────────────────────────────────────────────────────────────────┘

LocalModArchiveInstaller.InstallAsync()
         │
         │ After extraction and manifest loading:
         │
         ▼
┌─────────────────────────────────────────────────────────────────┐
│ _manifestService.GetResources(manifest, modRoot, gameRoot)      │
│ Returns: IEnumerable<(ResourceType, ResourceDescriptor)>        │
│   [                                                             │
│     (Asset, Descriptor { Source: ".../textures", ... }),        │
│     (Plugin, Descriptor { Source: ".../plugin.dll", ... }),     │
│     (Patcher, Descriptor { Source: ".../patcher.dll", ... })    │
│   ]                                                             │
└─────────────────────────────────────────────────────────────────┘
         │
         │ Injected via DI: IEnumerable<IResourceHandler>
         │   [AssetResourceHandler, PluginResourceHandler, PatcherResourceHandler]
         │
         ▼
┌─────────────────────────────────────────────────────────────────┐
│ For each (type, descriptor) in resources:                       │
│                                                                 │
│   handler = _resourceHandlers.First(h => h.SupportedType == type)
│   await handler.HandleAsync(descriptor, context, ct)            │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │ AssetResourceHandler.HandleAsync()                      │  │
│   │   - Validates source is directory                       │  │
│   │   - Creates destination if needed                       │  │
│   │   - Directory.Move(source, destination)                 │  │
│   └─────────────────────────────────────────────────────────┘  │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │ PluginResourceHandler.HandleAsync()                     │  │
│   │   - Validates source is file                            │  │
│   │   - Gets plugin directory from manifest service         │  │
│   │   - File.Move(source, pluginDir/filename)               │  │
│   └─────────────────────────────────────────────────────────┘  │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐  │
│   │ PatcherResourceHandler.HandleAsync()                    │  │
│   │   - Validates source is file                            │  │
│   │   - Gets patcher directory                              │  │
│   │   - File.Move(source, patcherDir/filename)              │  │
│   └─────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
         │
         │ To add NEW resource type (e.g., Shader):
         │
         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 1. Add Shader to ResourceType enum                              │
│ 2. Create ShaderResourceHandler : IResourceHandler              │
│ 3. Register in DI container                                     │
│    services.AddTransient<IResourceHandler, ShaderResourceHandler>()
│                                                                 │
│ NO CHANGES to LocalModArchiveInstaller or other handlers!       │
└─────────────────────────────────────────────────────────────────┘
```

### 6.5 Mod Source Abstraction (GameBanana → GitHub)

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    MOD SOURCE ABSTRACTION LAYER                          │
└──────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ IModClientAccessor (Interface)                                  │
│   - PlatformName: string                                        │
│   - GetModByIdAsync(string modId): Task<Result<ModItem>>        │
│   - SearchModsAsync(string term, int page): Task<Result<...>>   │
│   - GetFileMetadataAsync(string fileId): Task<Result<...>>      │
└─────────────────────────────────────────────────────────────────┘
         ▲                                    ▲
         │                                    │
         │ Implements                         │ Implements
         │                                    │
┌────────────────────────┐      ┌────────────────────────────────┐
│ GameBananaClientAccessor│      │ GitHubClientAccessor (Future)  │
│                        │      │                                │
│ PlatformName =         │      │ PlatformName = "GitHub"        │
│   "GameBanana"         │      │                                │
│                        │      │ GetModByIdAsync(repo, release) │
│ GetModByIdAsync(id)    │      │   └─► GitHub REST API          │
│   └─► GB API v12       │      │                                │
│                        │      │ SearchModsAsync(query)         │
│ SearchModsAsync(term)  │      │   └─► GitHub GraphQL API       │
│   └─► GB Index API     │      │                                │
│                        │      │ GetFileMetadataAsync(asset)    │
│ GetFileMetadataAsync   │      │   └─► GitHub Releases API      │
│   └─► GB File API      │      │                                │
└────────────────────────┘      └────────────────────────────────┘

Usage in Orchestrator:
──────────────────────
public async Task<OrchestrationResult> InstallFromSourceAsync(
    IModClientAccessor source,  ← Works with ANY implementation
    string modId,
    string fileId,
    ...)
{
    var fileResult = await source.GetFileMetadataAsync(fileId);
    // Rest of pipeline is source-agnostic
}

Adding Nexus Mods (Future):
───────────────────────────
1. Create NexusModsClientAccessor : IModClientAccessor
2. Implement methods using Nexus API
3. Register in DI: services.AddTransient<NexusModsClientAccessor>()
4. Use in ViewModel based on mod source detection

NO CHANGES to orchestrator, installer, or download logic!
```

---

## 7. Migration Strategy

### Phase 1: Foundation (High Priority)

**Goal:** Establish core abstractions without breaking existing functionality.

#### Step 1.1: Create Core Interfaces
- [ ] `IModClientAccessor`
- [ ] `IModContentDownloader`
- [ ] `ILocalModInstaller`
- [ ] `IModRegistrationService`
- [ ] `IModInstallationOrchestrator`

#### Step 1.2: Extract GameBanana Implementation
- [ ] Create `GameBananaClientAccessor` from `GamebananaApiService` (data methods only)
- [ ] Create `HttpContentDownloader` from `GamebananaApiService` (download methods)
- [ ] Keep `GamebananaApiService` as facade delegating to both (temporary)

#### Step 1.3: Extract Local Installer
- [ ] Create `LocalModArchiveInstaller` from `ModInstaller`
- [ ] Remove `ProfileManager` dependency
- [ ] Keep `ModInstaller` as facade delegating to new class (temporary)

#### Step 1.4: Create Registration Service
- [ ] Create `ProfileRegistrationService` extracting profile logic from `ModInstaller`
- [ ] Inject into orchestrator, not installer

#### Step 1.5: Create Orchestrator
- [ ] Implement `ModInstallationOrchestrator`
- [ ] Wire up all dependencies
- [ ] Update `ModSelectionDialogViewModel` to use orchestrator

**Estimated Effort:** 3-4 days  
**Risk:** Medium (requires careful testing of each extraction)  
**Rollback Plan:** Keep facades (`GamebananaApiService`, `ModInstaller`) forwarding to new implementations

### Phase 2: Extensibility (Medium Priority)

**Goal:** Enable easy addition of new resource types and sort strategies.

#### Step 2.1: Implement Resource Handler Strategy
- [ ] Create `IResourceHandler` interface
- [ ] Create `AssetResourceHandler`, `PluginResourceHandler`, `PatcherResourceHandler`
- [ ] Update `LocalModArchiveInstaller` to use strategy pattern
- [ ] Register handlers in DI

#### Step 2.2: Implement Sort Strategy
- [ ] Create `ISortStrategy` interface
- [ ] Create concrete strategies for each `FilterTypes` enum value
- [ ] Update `ModSelectionDialogViewModel` to use strategies
- [ ] Enable composite strategies

#### Step 2.3: Create Temp Directory Factory
- [ ] Create `ITempDirectoryFactory` interface
- [ ] Implement `TempDirectoryFactory`
- [ ] Replace direct `GameEnvironmentController.CreateTempSubdirectory()` calls

**Estimated Effort:** 2-3 days  
**Risk:** Low (additive changes, existing functionality preserved)

### Phase 3: Testability (Medium Priority)

**Goal:** Enable comprehensive unit testing.

#### Step 3.1: Replace Static Utilities
- [ ] Create `IModManifestService` replacing `ModManifestUtils`
- [ ] Create `IModThumbnailService` for image handling
- [ ] Update all consumers

#### Step 3.2: Add Unit Tests
- [ ] Test `ModInstallationOrchestrator` with mocked dependencies
- [ ] Test `LocalModArchiveInstaller` with fake file system
- [ ] Test resource handlers with isolated scenarios
- [ ] Test sort strategies with known inputs/outputs

#### Step 3.3: Integration Tests
- [ ] End-to-end installation test with real GameBanana API (staging)
- [ ] Batch installation concurrency test
- [ ] Cancellation token propagation test

**Estimated Effort:** 3-4 days  
**Risk:** Low (tests are additive)

### Phase 4: Polish (Low Priority)

**Goal:** Complete migration and remove legacy code.

#### Step 4.1: Remove Facades
- [ ] Remove `GamebananaApiService` (replace with `GameBananaClientAccessor` + `HttpContentDownloader`)
- [ ] Remove `ModInstaller` (replace with `LocalModArchiveInstaller`)

#### Step 4.2: Logging Migration
- [ ] Ensure all services use `ILogger<T>` consistently
- [ ] Add structured logging for diagnostics

#### Step 4.3: Documentation
- [ ] XML documentation for all interfaces
- [ ] Architecture decision records (ADRs)
- [ ] Developer guide for adding new mod sources

**Estimated Effort:** 1-2 days  
**Risk:** Low (cleanup phase)

### Migration Timeline

```
Week 1: Phase 1 (Foundation)
  Days 1-2: Create interfaces, extract GameBanana implementation
  Days 3-4: Extract installer, create registration service
  Day 5:   Create orchestrator, update ViewModel, test

Week 2: Phase 2 (Extensibility)
  Days 1-2: Implement resource handler strategy
  Days 3-4: Implement sort strategy, temp factory
  Day 5:   Test extensibility (add dummy handler)

Week 3: Phase 3 (Testability)
  Days 1-2: Replace static utilities
  Days 3-5: Write unit and integration tests

Week 4: Phase 4 (Polish)
  Days 1-2: Remove facades, clean up
  Days 3-4: Logging, documentation
  Day 5:   Final review, merge
```

---

## 8. Testing Strategy

### 8.1 Before Refactoring: Limited Testability

```csharp
// BEFORE: Hard to test due to concrete dependencies
[Test]
public async Task InstallModArchiveAsync_Success()
{
    // Problem: Must construct real GameEnvironmentController, ProfileManager, etc.
    var controller = new GameEnvironmentController(...); // Needs real file system
    var profileManager = new ProfileManager(...); // Needs real profile storage
    var installer = new ModInstaller(
        NullLogger.Instance,
        new ModArchiveExtractor(),
        new ManifestLoader(),
        // ... 5 more concrete dependencies
        profileManager,
        controller,
        new SettingsService());
    
    // Test requires actual mod archive file
    var result = await installer.InstallModArchiveAsync("test.zip");
    
    // Hard to assert intermediate steps (extraction, scanning, etc.)
    Assert.IsTrue(result.Success);
}
```

**Problems:**
- Slow (real file system access)
- Non-isolated (profile state affects test)
- Brittle (depends on external state)
- Cannot mock intermediate steps

### 8.2 After Refactoring: Comprehensive Testability

```csharp
// AFTER: Easy to test with mocks
[Test]
public async Task InstallFromSourceAsync_DownloadFails_ReturnsFailure()
{
    // Arrange: Mock all dependencies
    var mockDownloader = new Mock<IModContentDownloader>();
    mockDownloader.Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), ...))
        .ReturnsAsync(Result<DownloadedFile>.Failure("Network error"));
    
    var mockInstaller = new Mock<ILocalModInstaller>();
    var mockRegistration = new Mock<IModRegistrationService>();
    var mockTempFactory = new Mock<ITempDirectoryFactory>();
    
    var orchestrator = new ModInstallationOrchestrator(
        mockDownloader.Object,
        mockInstaller.Object,
        mockRegistration.Object,
        mockTempFactory.Object,
        NullLogger.Instance);
    
    // Act
    var result = await orchestrator.InstallFromSourceAsync(
        new GameBananaClientAccessor(...),
        "600001",
        "1001",
        new InstallationOptions());
    
    // Assert
    Assert.IsFalse(result.Success);
    Assert.AreEqual("Download failed: Network error", result.ErrorMessage);
    mockInstaller.Verify(i => i.InstallAsync(It.IsAny<string>(), ...), Times.Never);
    // Installer never called because download failed
}

[Test]
public async Task InstallFromSourceAsync_FullPipeline_Success()
{
    // Arrange
    var mockDownloader = new Mock<IModContentDownloader>();
    mockDownloader.Setup(d => d.DownloadAsync(...))
        .ReturnsAsync(Result<DownloadedFile>.Success(
            new DownloadedFile("/temp/mod.zip", 1024, "http://...")));
    
    var mockInstaller = new Mock<ILocalModInstaller>();
    mockInstaller.Setup(i => i.InstallAsync(...))
        .ReturnsAsync(new InstallationResult 
        { 
            Success = true, 
            InstalledManifest = new ModManifest { Id = "test" } 
        });
    
    var mockRegistration = new Mock<IModRegistrationService>();
    mockRegistration.Setup(r => r.GetActiveProfileName())
        .Returns("Default");
    mockRegistration.Setup(r => r.RegisterModAsync(It.IsAny<ModManifest>(), "Default", ...))
        .ReturnsAsync(Result<Unit>.Success(Unit.Value));
    
    var orchestrator = new ModInstallationOrchestrator(
        mockDownloader.Object,
        mockInstaller.Object,
        mockRegistration.Object,
        new FakeTempDirectoryFactory(),
        NullLogger.Instance);
    
    // Act
    var result = await orchestrator.InstallFromSourceAsync(
        new GameBananaClientAccessor(...),
        "600001",
        "1001",
        new InstallationOptions { ActivateAfterInstall = true });
    
    // Assert
    Assert.IsTrue(result.Success);
    mockDownloader.Verify(d => d.DownloadAsync(...), Times.Once);
    mockInstaller.Verify(i => i.InstallAsync(...), Times.Once);
    mockRegistration.Verify(r => r.RegisterModAsync(...), Times.Once);
}

[Test]
public async Task AssetResourceHandler_HandleAsync_ValidDirectory_Move succeeds()
{
    // Arrange
    var handler = new AssetResourceHandler();
    var tempDir = CreateTempDirectory();
    var sourceDir = Path.Combine(tempDir, "source");
    var destDir = Path.Combine(tempDir, "dest");
    Directory.CreateDirectory(sourceDir);
    File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "content");
    
    var descriptor = new ResourceDescriptor
    {
        Type = ResourceType.Asset,
        SourcePath = sourceDir,
        DestinationPath = destDir
    };
    
    var context = new InstallationContext
    {
        Logger = NullLogger.Instance,
        // ... other properties
    };
    
    // Act
    var result = await handler.HandleAsync(descriptor, context);
    
    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.IsFalse(Directory.Exists(sourceDir)); // Moved
    Assert.IsTrue(Directory.Exists(destDir));
    Assert.IsTrue(File.Exists(Path.Combine(destDir, "test.txt")));
}
```

**Benefits:**
- Fast (no real file system unless testing file operations)
- Isolated (each test controls its own state)
- Verifiable (can assert which methods were called)
- Parallelizable (no shared state)

### 8.3 Test Coverage Goals

| Component | Target Coverage | Key Scenarios |
|-----------|----------------|---------------|
| `ModInstallationOrchestrator` | 90% | Download fail, install fail, registration fail, cancellation, batch processing |
| `LocalModArchiveInstaller` | 85% | Extraction fail, manifest null, security scan fail, resource install fail |
| Resource Handlers | 95% | Valid paths, missing source, permission errors, overwrite scenarios |
| `GameBananaClientAccessor` | 80% | API success, API failure, timeout, malformed response |
| `HttpContentDownloader` | 85% | Download success, network error, cancellation, progress reporting |
| Sort Strategies | 100% | Each strategy with known input/output, composite ordering |

---

## 9. Additional Architectural Concerns

### 9.1 Logging Abstraction

**Current State:** Mixed usage of `Serilog.Log.Logger` static calls and injected `ILogger`.

**Recommendation:** Standardize on `ILogger<T>` for all services:

```csharp
// Instead of:
Log.Logger.Information("Message");

// Use:
public sealed class MyService(ILogger<MyService> logger)
{
    private readonly ILogger<MyService> _logger = logger;
    
    public void DoWork() => _logger.LogInformation("Message");
}
```

**Benefits:**
- Testable with `NullLogger<T>` or mocks
- Automatic category tagging
- Consistent with ASP.NET Core patterns

### 9.2 Temporary Directory Management

**Current State:** `GameEnvironmentController.CreateTempSubdirectory()` creates nested temp directories inefficiently.

**Recommendation:** Centralized `ITempDirectoryFactory`:

```csharp
public interface ITempDirectoryFactory
{
    IDisposableTemporaryDirectory Create(string prefix = "gmp_");
}

public sealed class TempDirectoryFactory : ITempDirectoryFactory
{
    private readonly string _basePath;
    
    public TempDirectoryFactory()
    {
        _basePath = Path.Combine(Path.GetTempPath(), "GottaManagePlus");
        Directory.CreateDirectory(_basePath);
    }
    
    public IDisposableTemporaryDirectory Create(string prefix = "gmp_")
    {
        var path = Path.Combine(_basePath, $"{prefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return new DisposableTempDirectory(path);
    }
}

public sealed class DisposableTempDirectory : IDisposableTemporaryDirectory
{
    public DirectoryInfo DirectoryInfo { get; }
    public string FullPath { get; }
    
    public DisposableTempDirectory(string path)
    {
        FullPath = path;
        DirectoryInfo = new DirectoryInfo(path);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(FullPath))
        {
            try { Directory.Delete(FullPath, true); }
            catch { /* Log but don't throw */ }
        }
    }
}
```

### 9.3 Error Handling Consistency

**Current State:** Mixed patterns (`Result<T>`, `null` returns, exceptions).

**Recommendation:** Standardize on `Result<T>` for expected failures, exceptions for unexpected errors:

```csharp
// Expected failures (network errors, validation, etc.):
public Task<Result<ModItem>> GetModByIdAsync(...)

// Unexpected errors (bugs, corruption):
throw new InvalidOperationException("Manifest is corrupted");

// Caller handles both:
var result = await accessor.GetModByIdAsync(id);
if (result.IsFailure)
{
    // Expected failure (show user-friendly message)
    ShowError(result.Error);
    return;
}

try
{
    var mod = result.Value;
    // Process mod
}
catch (InvalidOperationException ex)
{
    // Unexpected error (log, show generic error)
    _logger.Error(ex, "Unexpected error processing mod");
    ShowError("An unexpected error occurred.");
}
```

### 9.4 Configuration Management

**Current State:** `SettingsService` injected directly into many classes.

**Recommendation:** Use options pattern for install-specific settings:

```csharp
public record InstallationSettings
{
    public bool ScanForSecurityIssues { get; init; } = true;
    public bool CancelOnSecurityIssues { get; init; } = false;
    public int MaxConcurrentDownloads { get; init; } = 3;
    public TimeSpan DownloadTimeout { get; init; } = TimeSpan.FromMinutes(10);
}

// Registration:
services.Configure<InstallationSettings>(
    config.Bind("Installation"));

// Usage:
public sealed class ModInstallationOrchestrator(
    IOptions<InstallationSettings> options,
    ...)
{
    private readonly InstallationSettings _settings = options.Value;
    
    // Use _settings.MaxConcurrentDownloads, etc.
}
```

---

## Appendix A: Complete Service Dependency Graph

```
┌──────────────────────────────────────────────────────────────────────────┐
│                    FINAL SERVICE DEPENDENCY GRAPH                        │
└──────────────────────────────────────────────────────────────────────────┘

ModSelectionDialogViewModel
    ├── IModInstallationOrchestrator
    │   ├── IModContentDownloader
    │   │   ├── IHttpClientFactory
    │   │   └── ITempDirectoryFactory
    │   ├── ILocalModInstaller
    │   │   ├── ILogger<LocalModArchiveInstaller>
    │   │   ├── IModArchiveExtractor
    │   │   ├── IManifestLoader
    │   │   ├── ISecurityScanner
    │   │   ├── IEnumerable<IResourceHandler>
    │   │   │   ├── AssetResourceHandler
    │   │   │   ├── PluginResourceHandler
    │   │   │   │   └── IModManifestService
    │   │   │   └── PatcherResourceHandler
    │   │   └── IModManifestService
    │   ├── IModRegistrationService
    │   │   ├── ProfileRepository
    │   │   └── ILogger<ProfileRegistrationService>
    │   ├── ITempDirectoryFactory
    │   └── ILogger<ModInstallationOrchestrator>
    └── IDialogService

GameBananaClientAccessor (implements IModClientAccessor)
    └── IHttpClientFactory

HttpContentDownloader (implements IModContentDownloader)
    ├── IHttpClientFactory
    └── ITempDirectoryFactory

LocalModArchiveInstaller (implements ILocalModInstaller)
    ├── ILogger
    ├── IModArchiveExtractor
    ├── IManifestLoader
    ├── ISecurityScanner
    ├── IEnumerable<IResourceHandler>
    └── IModManifestService

ProfileRegistrationService (implements IModRegistrationService)
    ├── ProfileRepository
    └── ILogger

ModInstallationOrchestrator (implements IModInstallationOrchestrator)
    ├── IModContentDownloader
    ├── ILocalModInstaller
    ├── IModRegistrationService
    ├── ITempDirectoryFactory
    └── ILogger

TOTAL SERVICES:
  Interfaces: 11
  Implementations: 13
  Total: 24 services (vs. ~8 monolithic classes before)
```

---

## Appendix B: Glossary

| Term | Definition |
|------|------------|
| **Mod Client** | A service that interacts with a mod hosting platform (GameBanana, GitHub, etc.) |
| **Orchestrator** | A service that coordinates multiple other services to complete a complex workflow |
| **Resource Handler** | A strategy implementation that knows how to install a specific type of mod resource |
| **Manifest** | A JSON file describing a mod's structure, including plugins, assets, and metadata |
| **Profile** | A saved state of installed mods and configuration that can be switched |
| **Result\<T\>** | A discriminated union type representing either success (with value) or failure (with error) |

---

## Conclusion

The current mod installation pipeline violates all five SOLID principles, creating a tightly coupled, hard-to-test, and inflexible architecture. The proposed refactoring addresses these issues by:

1. **Abstracting mod sources** behind `IModClientAccessor` for future GitHub/Nexus integration
2. **Separating orchestration** from installation with `IModInstallationOrchestrator`
3. **Emphasizing local installation** with renamed `ILocalModInstaller` / `LocalModArchiveInstaller`
4. **Enabling extensibility** via strategy patterns for resource handling and sorting
5. **Improving testability** through interface-based dependencies

The migration can be completed in 4 weeks with minimal risk by following the phased approach, keeping facades during transition, and maintaining backward compatibility throughout.

**Next Steps:**
1. Review this report with the team
2. Prioritize phases based on upcoming features (e.g., GitHub integration urgency)
3. Create tickets for Phase 1 tasks
4. Begin implementation

---

*Report generated: June 2026*  
*Author: SOLID Principles Analysis*  
*Review Status: Pending Team Review*
