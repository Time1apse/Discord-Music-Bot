using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Music_Bot.Core.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Victoria;

namespace Discord_Music_Bot.Core;

public class Bot
{
    private DiscordSocketClient _client;
    private CommandService _commandService;

    public Bot()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            LogLevel = LogSeverity.Debug
        });

        _commandService = new CommandService(new CommandServiceConfig()
        {
            LogLevel = LogSeverity.Debug,
            CaseSensitiveCommands = false,
            DefaultRunMode = RunMode.Async,
            IgnoreExtraArgs = true
        });

        var collection = new ServiceCollection();
        collection.AddSingleton(_client);
        collection.AddSingleton(_commandService);
        collection.AddLavaNode(x =>
        {
            x.SelfDeaf = false;
            x.Authorization = "youshallnotpass";
            x.Port = 2333;
            x.Hostname = "127.0.0.1";
        });
        
        ServiceManager.SetProvider(collection);
    }

    public async Task MainAsync()
    {
        if (string.IsNullOrWhiteSpace((ConfigManager.Config.token))) return;

        await EventManager.LoadCommands();
        await CommandManager.LoadCommandsAsync();
        await _client.LoginAsync(TokenType.Bot, ConfigManager.Config.token);
        await _client.StartAsync();

        await Task.Delay(-1);
    }
}