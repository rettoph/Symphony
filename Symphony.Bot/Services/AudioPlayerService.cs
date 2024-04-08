using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Symphony.Bot.Services
{
    public class AudioPlayerService
    {
        private readonly IYouTubeAudioOutputProviderService _outputProvider;
        private readonly IOptions<AudioPlayerOptions> _options;
        private readonly Dictionary<ulong, AudioPlayer> _audioPlayers = new Dictionary<ulong, AudioPlayer>();

        public AudioPlayerService(IYouTubeAudioOutputProviderService outputProvider, IOptions<AudioPlayerOptions> options)
        {
            _outputProvider = outputProvider;
            _options = options;
            _audioPlayers = new Dictionary<ulong, AudioPlayer>();
        }

        public void Add(SocketGuild guild)
        {
            _audioPlayers.Add(guild.Id, new AudioPlayer(guild, _options, _outputProvider));
        }

        public async Task Enqueue(SocketTextChannel text, SocketVoiceChannel? voice, string videoId)
        {
            ulong guildId = voice?.Guild.Id ?? text.Guild.Id;

            if (_audioPlayers.TryGetValue(guildId, out AudioPlayer? player) == false)
            {
                return;
            }

            await player.Enqueue(text, voice, videoId);
        }

        public async Task Skip(ulong guildId)
        {
            if (_audioPlayers.TryGetValue(guildId, out AudioPlayer? player) == false)
            {
                return;
            }

            await player.Skip();
        }
    }
}
