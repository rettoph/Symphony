using Discord.Commands;
using Discord.WebSocket;
using Symphony.Bot.Services;

namespace Symphony.Bot.Modules
{
    public class AudioPlayerModule : ModuleBase<SocketCommandContext>
    {
        private readonly AudioPlayerService _audioPlayers;

        public AudioPlayerModule(AudioPlayerService audioPlayers)
        {
            _audioPlayers = audioPlayers;
        }

        [Command("ping")]
        [Alias("pong", "hello")]
        public Task PingAsync()
            => ReplyAsync("pong!");

        [Command("play")]
        public async Task Play(string videoId)
        {
            var user = this.Context.Guild.GetUser(this.Context.Message.Author.Id);
            await _audioPlayers.Enqueue(this.Context.Message.Channel as SocketTextChannel ?? throw new Exception("Uh oh, stinky text channel"), user.VoiceChannel, videoId);
        }

        [Command("skip")]
        public async Task Skip()
        {
            await _audioPlayers.Skip(this.Context.Guild.Id);
        }
    }
}
