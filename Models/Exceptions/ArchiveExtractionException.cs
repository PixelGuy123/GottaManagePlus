using System;

namespace GottaManagePlus.Models.Exceptions;

public class ArchiveExtractionException(string archivePath, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public string ArchivePath { get; } = archivePath;
}