using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Symphony.Bot.Services
{
    internal class DiscordLifecycleService : IHostedService
    {
        private readonly IOptions<DiscordOptions> _options;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ILogger<DiscordLifecycleService> _logger;
        private readonly AudioPlayerService _audioPlayers;
        private readonly CommandService _commands;

        public DiscordLifecycleService(IOptions<DiscordOptions> options, DiscordSocketClient client, IServiceProvider services, ILogger<DiscordLifecycleService> logger, AudioPlayerService audioPlayers, CommandService commands)
        {
            _options = options;
            _client = client;
            _services = services;
            _logger = logger;
            _audioPlayers = audioPlayers;
            _commands = commands;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _commands.AddModulesAsync(typeof(DiscordLifecycleService).Assembly, _services);

            await this.Login();
            await _client.StartAsync();

            _client.Disconnected += this.HandleDisconnected;
            _client.MessageReceived += this.HandleMessageRecieved;

            _client.GuildAvailable += (x) =>
            {
                _audioPlayers.Add(x);

                return Task.CompletedTask;
            };
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
                await _commands.ExecuteAsync(context, argPos, scope.ServiceProvider);
            }

            // Note that normally a result will be returned by this format, but here
            // we will handle the result in CommandExecutedAsync,
        }
    }
}
