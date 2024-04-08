using System.Diagnostics;

namespace Symphony.Bot.Services
{
    internal class LinuxYouTubeAudioOutputProviderService : IYouTubeAudioOutputProviderService
    {
        public Process GetAudioOutput(string videoId, float volume)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "sh",
                Arguments = $"-c \"youtube-dlp -x https://www.youtube.com/watch?v={videoId} -o - | ffmpeg -hide_banner -loglevel panic -i pipe: -ac 2 -f s16le -ar 48000 -filter:a \"volume={volume}\" pipe:1\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            }) ?? throw new Exception("Uh oh, stinky audio output");
        }
    }
}
