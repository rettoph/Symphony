using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Symphony.Bot.Enums;
using Symphony.Bot.Services;

namespace Symphony.Bot
{
    internal class AudioPlayer
    {
        private readonly SocketGuild _guild;
        private readonly IOptions<AudioPlayerOptions> _options;
        private readonly IYouTubeAudioOutputProviderService _outputProvider;
        private readonly Queue<Task> _queue;
        private readonly SemaphoreSlim _semaphore;
        private CancellationTokenSource? _currentCancelationTokenSource;

        private SocketVoiceChannel? _voice;
        private IAudioClient? _client;

        public AudioPlayerStatus Status { get; private set; }

        public AudioPlayer(SocketGuild guild, IOptions<AudioPlayerOptions> options, IYouTubeAudioOutputProviderService outputProvider)
        {
            _guild = guild;
            _options = options;
            _outputProvider = outputProvider;
            _queue = new Queue<Task>();
            _semaphore = new SemaphoreSlim(1);
        }

        public async Task Skip()
        {
            if (_currentCancelationTokenSource is null)
            {
                return;
            }

            await _currentCancelationTokenSource.CancelAsync();
        }

        public async Task Enqueue(SocketTextChannel text, SocketVoiceChannel? voice, string videoId)
        {
            _voice ??= voice;
            voice ??= _voice ?? throw new Exception();

            CancellationTokenSource cts = new CancellationTokenSource();
            IMessage message = await text.SendMessageAsync($"Added {videoId} to queue ({(_queue.Count + 1)})");

            _queue.Enqueue(new Task(async () =>
            {
                try
                {
                    await _semaphore.WaitAsync();

                    this.Status = AudioPlayerStatus.Playing;

                    if (_voice is not null && _voice.Id != voice.Id && _client is not null && _client.ConnectionState != ConnectionState.Disconnected)
                    {
                        await _voice.DisconnectAsync();
                        _client = null;
                    }

                    IAudioClient? client = _client;
                    if (_client is null)
                    {
                        client = _client = await voice.ConnectAsync();
                    }

                    if (client is null)
                    {
                        await text.SendMessageAsync("Uh oh, stinky voice channel.");
                        return;
                    }


                    using (var ffmpeg = _outputProvider.GetAudioOutput(videoId, _options.Value.Volume))
                    using (var output = ffmpeg.StandardOutput.BaseStream)
                    using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
                    {
                        try
                        {
                            _currentCancelationTokenSource = cts;

                            await text.ModifyMessageAsync(message.Id, ctx => ctx.Content = $"Now playing {videoId}");
                            await output.CopyToAsync(discord, cts.Token);
                        }
                        catch (Exception ex)
                        {
                            await text.ModifyMessageAsync(message.Id, ctx => ctx.Content = $"{ex}");
                            await Task.Delay(5000);
                        }
                        finally
                        {
                            await discord.FlushAsync();

                            await text.DeleteMessageAsync(message.Id);
                        }
                    }
                }
                finally
                {
                    this.Status = AudioPlayerStatus.Connected;

                    _semaphore.Release();
                }
            }));

            _ = Task.Run(async () =>
            {
                try
                {
                    await _semaphore.WaitAsync();

                    _queue.Dequeue().Start();
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }
    }
}
