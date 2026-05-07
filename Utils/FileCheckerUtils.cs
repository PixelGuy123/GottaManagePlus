using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using GottaManagePlus.Models.FileTypes;

namespace GottaManagePlus.Utils;

public static class FileCheckerUtils
{
    extension(Stream fileContent)
    {
        public async Task<bool> IsAudio(CancellationToken cancellationToken = default)
            => await fileContent.IsAsync<Mp3>(cancellationToken)
               || await fileContent.IsAsync<WaveformAudioFileFormat>(cancellationToken)
               || await fileContent.IsAsync<M4V>(cancellationToken)
               || await fileContent.IsAsync<WindowsAudio>(cancellationToken)
               || await fileContent.IsAsync<MpegAudio>(cancellationToken)
               || await fileContent.IsAsync<AudioVideoInterleaveVideoFormat>(cancellationToken)
               || await fileContent.IsAsync<Ogg>(cancellationToken);

        public async Task<bool> IsVideo(CancellationToken cancellationToken = default)
            => await fileContent.IsAsync<Mp4>(cancellationToken)
               || await fileContent.IsAsync<AudioVideoInterleaveVideoFormat>(cancellationToken);
    }
}