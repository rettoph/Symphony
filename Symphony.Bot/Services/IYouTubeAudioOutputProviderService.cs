using System.Diagnostics;

namespace Symphony.Bot.Services
{
    public interface IYouTubeAudioOutputProviderService
    {
        Process GetAudioOutput(string videoId, float volume);
    }
}
