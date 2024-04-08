using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Symphony.Bot;
using Symphony.Bot.Services;

// https://discord.com/api/oauth2/authorize?client_id=1226759238827905095&permissions=8&scope=bot%20applications.commands

await Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(configuration =>
    {
        configuration.AddJsonFile("appsettings.json");
    })
    .ConfigureServices((context, services) =>
    {
        services.AddOptions();

        services.AddSingleton<DiscordSocketClient>()
            .AddHostedService<DiscordLifecycleService>()
            .AddSingleton(Options.Create(context.Configuration.GetSection("Discord").Get<DiscordOptions>() ?? new DiscordOptions()));
    })
    .Build()
    .RunAsync();