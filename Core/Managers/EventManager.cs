using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using System.Xml;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace Discord_Music_Bot.Core.Managers;

public static class EventManager
{
    private static LavaNode _lavaNode = ServiceManager.Provider.GetRequiredService<LavaNode>();
    private static DiscordSocketClient _client = ServiceManager.GetService<DiscordSocketClient>();
    private static CommandService _commandService = ServiceManager.GetService<CommandService>();


    public static Task LoadCommands()
    {
        _client.Log += message =>
        {
            Console.WriteLine($"[{DateTime.Now}]\t{message.Source}\t{message.Message}");
            return Task.CompletedTask;
        };
        
        _commandService.Log += message => {
            Console.WriteLine($"[{DateTime.Now}]\t{message.Source}\t{message.Message}");
            return Task.CompletedTask;
        };

        _client.Ready += ClientOnReady;

        _client.MessageReceived += OnMessageRecieved;
        
        return Task.CompletedTask;
    }

    private static async Task OnMessageRecieved(SocketMessage msg)
    {
        var message = msg as SocketUserMessage;
        var contex = new SocketCommandContext(_client, message);

        if (message.Author.IsBot || message.Channel is IDMChannel) return;

        var argPos = 0;
        
        if(!(message.HasStringPrefix(ConfigManager.Config.prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

        var result = await _commandService.ExecuteAsync(contex, argPos, ServiceManager.Provider);

        if (!result.IsSuccess)
        {
            if (result.Error == CommandError.UnknownCommand)
                await message.Channel.SendMessageAsync("Долбаеб пиши команды правильно и те, которые имеются");
        }
    }
    
    private static async Task ClientOnReady()
    {
        try
        {
            await _lavaNode.ConnectAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        Console.WriteLine($"{DateTime.Now}\t(BRUH)\tBot is chilling");
        await _client.SetStatusAsync(UserStatus.DoNotDisturb);
        await _client.SetGameAsync($"Prefix: {ConfigManager.Config.prefix}", null, ActivityType.Listening);
    }
}