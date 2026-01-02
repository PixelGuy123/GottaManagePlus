using System.IO;
using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using GottaManagePlus.Models.FileTypes;

namespace GottaManagePlus.Utils;

public static class FileCheckerUtils
{
    public static bool IsAudio(Stream fileContent)
        => fileContent.Is<Mp3>()
        || fileContent.Is<WaveformAudioFileFormat>()
        || fileContent.Is<M4V>()
        || fileContent.Is<WindowsAudio>()
        || fileContent.Is<MpegAudio>()
        || fileContent.Is<AudioVideoInterleaveVideoFormat>()
        || fileContent.Is<Ogg>();

    public static bool IsVideo(Stream fileContent)
        => fileContent.Is<Mp4>()
           || fileContent.Is<AudioVideoInterleaveVideoFormat>();
}