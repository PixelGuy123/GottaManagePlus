/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using ByteSizeLib;
using GottaManagePlus.Interfaces.GameEnvironment;
using Tomlyn.Serialization;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace GottaManagePlus.Models;

/// <summary>
/// Represents a snapshot of the game environment's folder structure.
/// </summary>
public class EnvironmentSnapshot : IEquatable<EnvironmentSnapshot>
{
    // ----- Public -----
    /// <summary>
    /// UTC timestamp when the snapshot was taken.
    /// </summary>
    [TomlRequired]
    public DateTime CreationTimeUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// All directories relative to <see cref="IGameEnvironment.RootPath"/>.
    /// </summary>
    [TomlRequired]
    public List<string> Directories { get; set; } = [];

    /// <summary>
    /// All files with their metadata.
    /// </summary>
    [TomlRequired]
    public List<SnapshotFileEntry> Files { get; set; } = [];
    
    /// <summary>
    /// Metadata for a single file in the snapshot.
    /// </summary>
    public struct SnapshotFileEntry : IEquatable<SnapshotFileEntry>
    {
        public SnapshotFileEntry()
        {
        }
        
        /// <summary>
        /// Path relative to <see cref="IGameEnvironment.RootPath"/>.
        /// </summary>
        public string? RelativePath { get; set; } = null;

        /// <summary>
        /// Last write time (UTC) of the file.
        /// </summary>
        public DateTime LastWriteTimeUtc { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// File size in bytes.
        /// </summary>
        public ByteSize SizeBytes { get; set; } = new(0);

        // ---- Equality API -----
        public override bool Equals(object? obj) => Equals(obj as SnapshotFileEntry?);
        public bool Equals(SnapshotFileEntry? other)
        {
            if (!other.HasValue) return false;
            
            return RelativePath == other.Value.RelativePath &&
                   LastWriteTimeUtc == other.Value.LastWriteTimeUtc &&
                   SizeBytes == other.Value.SizeBytes;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(RelativePath, LastWriteTimeUtc, SizeBytes);
        }

        public static bool operator ==(SnapshotFileEntry? left, SnapshotFileEntry? right) => EqualityComparer<SnapshotFileEntry>.Equals(left, right);
        public static bool operator !=(SnapshotFileEntry? left, SnapshotFileEntry? right) => !(left == right);

        public bool Equals(SnapshotFileEntry other) => RelativePath == other.RelativePath && LastWriteTimeUtc.Equals(other.LastWriteTimeUtc) && SizeBytes == other.SizeBytes;
    }

    // ---- Equality API ----
    public override bool Equals(object? obj) => Equals(obj as EnvironmentSnapshot);
    public bool Equals(EnvironmentSnapshot? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return CreationTimeUtc == other.CreationTimeUtc &&
               AreDirectoriesEqual(other) &&
               AreFilesEqual(other);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CreationTimeUtc);
        
        foreach (var dir in Directories.OrderBy(d => d))
            hash.Add(dir);
        
        foreach (var file in Files.OrderBy(f => f.RelativePath))
            hash.Add(file);
        
        return hash.ToHashCode();
    }

    // ----- Private Equality API -----
    private bool AreDirectoriesEqual(EnvironmentSnapshot other) => Directories.OrderBy(d => d).SequenceEqual(other.Directories.OrderBy(d => d));
    private bool AreFilesEqual(EnvironmentSnapshot other) =>
        Files.OrderBy(f => f.RelativePath)
            .SequenceEqual(other.Files.OrderBy(f => f.RelativePath));
    public static bool operator ==(EnvironmentSnapshot? left, EnvironmentSnapshot? right) => EqualityComparer<EnvironmentSnapshot>.Default.Equals(left, right);
    public static bool operator !=(EnvironmentSnapshot? left, EnvironmentSnapshot? right) => !(left == right);
}