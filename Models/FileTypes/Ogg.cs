using FileTypeChecker;
using FileTypeChecker.Abstracts;

namespace GottaManagePlus.Models.FileTypes;

public class Ogg() : FileType(FName, FMimeType, FExtension, MagicBytes)
{
    private const string FName = "Ogging";
    private const string FExtension = "ogg";
    private const string FMimeType = "audio/ogg";
    private static readonly byte[] MagicBytes = "OggS"u8.ToArray();
}