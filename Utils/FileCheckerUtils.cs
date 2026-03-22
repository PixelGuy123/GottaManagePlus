using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using GottaManagePlus.Models.FileTypes;

namespace GottaManagePlus.Utils;

public static class FileCheckerUtils
{
    public static async Task<bool> IsAudio(this Stream fileContent, CancellationToken cancellationToken = default)
        => await fileContent.IsAsync<Mp3>(cancellationToken)
        || await fileContent.IsAsync<WaveformAudioFileFormat>(cancellationToken)
        || await fileContent.IsAsync<M4V>(cancellationToken)
        || await fileContent.IsAsync<WindowsAudio>(cancellationToken)
        || await fileContent.IsAsync<MpegAudio>(cancellationToken)
        || await fileContent.IsAsync<AudioVideoInterleaveVideoFormat>(cancellationToken)
        || await fileContent.IsAsync<Ogg>(cancellationToken);

    public static async Task<bool> IsVideo(this Stream fileContent,  CancellationToken cancellationToken = default)
        => await fileContent.IsAsync<Mp4>(cancellationToken)
           || await fileContent.IsAsync<AudioVideoInterleaveVideoFormat>(cancellationToken);
}