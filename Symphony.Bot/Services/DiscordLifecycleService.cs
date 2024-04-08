using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Symphony.Bot.Services
{
    internal class DiscordLifecycleService : IHostedService
    {
        private readonly IOptions<DiscordOptions> _options;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ILogger<DiscordLifecycleService> _logger;

        public DiscordLifecycleService(IOptions<DiscordOptions> options, DiscordSocketClient client, IServiceProvider services, ILogger<DiscordLifecycleService> logger)
        {
            _options = options;
            _client = client;
            _services = services;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.Login();
            await _client.StartAsync();

            _client.Disconnected += this.HandleDisconnected;
            _client.MessageReceived += this.HandleMessageRecieved;

            _client.GuildAvailable += async (x) =>
            {
                _ = Task.Run(async () =>
                {
                    var client = await x.VoiceChannels.First().ConnectAsync();

                    using (var ffmpeg = CreateStream("daB9QRwVQH4"))
                    using (var output = ffmpeg.StandardOutput.BaseStream)
                    using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
                    {
                        try { await output.CopyToAsync(discord); }
                        finally { await discord.FlushAsync(); }
                    }
                });

            };
        }

        private Process? CreateStream(string videoId)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"get-audio.sh {videoId}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task Login()
        {
            _logger.LogInformation("Attempting to login...");
            await _client.LoginAsync(TokenType.Bot, _options.Value.Token);
        }

        private async Task HandleDisconnected(Exception exception)
        {
            _logger.LogWarning("Connection lost...");

            await _client.StopAsync();
            await _client.StartAsync();

            await _client.LoginAsync(TokenType.Bot, _options.Value.Token);
        }

        private async Task HandleMessageRecieved(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message))
                return;
            if (message.Source != MessageSource.User)
                return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            // Perform prefix check. You may want to replace this with
            // (!message.HasCharPrefix('!', ref argPos))
            // for a more traditional command format like !help.
            if (!message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);
            // Perform the execution of the command. In this method,
            // the command service will perform precondition and parsing check
            // then execute the command if one is matched.
            using (IServiceScope scope = _services.CreateScope())
            {
                throw new NotImplementedException();
                //await _commands.ExecuteAsync(context, argPos, scope.ServiceProvider);
            }

            // Note that normally a result will be returned by this format, but here
            // we will handle the result in CommandExecutedAsync,
        }
    }
}
