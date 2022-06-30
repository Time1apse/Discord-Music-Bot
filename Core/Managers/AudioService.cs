using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Discord_Music_Bot.Core.Managers;

public sealed class AudioService
{
    private readonly LavaNode _lavaNode;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;

    public AudioService(LavaNode lavaNode, ILoggerFactory loggerFactory)
    {
        _lavaNode = lavaNode;
        _logger = loggerFactory.CreateLogger<LavaNode>();
        _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

        _lavaNode.OnLog += arg =>
        {
            _logger.Log(LogLevel.Debug, arg.Exception, arg.Message);
            return Task.CompletedTask;
        };

        _lavaNode.OnPlayerUpdated += OnPlayerUpdated;
        _lavaNode.OnStatsReceived += OnStatsRecieved;
        _lavaNode.OnTrackEnded += OnTrackEnded;
        _lavaNode.OnTrackStarted += OnTrackStarted;
        _lavaNode.OnTrackException += OnTrackException;
        _lavaNode.OnTrackStuck += OnTrackStuck;
        _lavaNode.OnWebSocketClosed += OnWebSocketClosed;
    }

    private Task OnPlayerUpdated(PlayerUpdateEventArgs arg)
    {
        _logger.LogInformation($"Трек: {arg.Track.Title}: {arg.Track.Position}");
        return Task.CompletedTask;
    }

    private Task OnStatsRecieved(StatsEventArgs arg)
    {
        _logger.LogInformation($"Lavalink включен уже {arg.Uptime}");
        return Task.CompletedTask;
    }

    private async Task OnTrackStarted(TrackStartEventArgs arg)
    {
        await arg.Player.TextChannel.SendMessageAsync($"Сейчас играет {arg.Track.Title}");
        if (!_disconnectTokens.TryGetValue(arg.Player.VoiceChannel.Id, out var value)) return;

        if (value.IsCancellationRequested) return;
        
        value.Cancel(true);
        await arg.Player.TextChannel.SendMessageAsync("Аудио дисконнект отменен");
    }

    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (args.Reason != TrackEndReason.Finished) return;

        var player = args.Player;
        if (!player.Queue.TryDequeue(out var lavaTrack)) {
            await player.TextChannel.SendMessageAsync("Очередь за сосиками закончилась, купи подписку и покупай сосиски без очереди");
            _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(10));
            return;
        }

        if (lavaTrack is null)
        {
            await player.TextChannel.SendMessageAsync(
                "Следующий объект в очереди не является треком, так же, как твои родители не являются тебе родными");
        }

        await args.Player.PlayAsync(lavaTrack);
        await args.Player.TextChannel.SendMessageAsync(
            $"{args.Reason}: {args.Track.Title}\nСейчас ебашит: {lavaTrack.Title}");
    }
    
    private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
    {
        if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
        {
            value = new CancellationTokenSource();
            _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
        }
        else if (value.IsCancellationRequested)
        {
            _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
            value = _disconnectTokens[player.VoiceChannel.Id];
        }

        await player.TextChannel.SendMessageAsync($"Авто дисконнект начался, через {timeSpan} секунд вам всем пиздец");
        var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
        if (isCancelled)
        {
            return;
        }

        await _lavaNode.LeaveAsync(player.VoiceChannel);
        await player.TextChannel.SendMessageAsync(
            "Если ты когда-нибудь меня снова не позовешь, я найду тебя и вырежу твою семью");
    }

    private async Task OnTrackException(TrackExceptionEventArgs arg)
    {
        _logger.LogError($"Трек {arg.Track.Title} вызвал ошибку, чекай логи");
        arg.Player.Queue.Enqueue(arg.Track);
        await arg.Player.TextChannel.SendMessageAsync($"{arg.Track.Title} передобавлен в очередь после вызова ошибки");
    }

    private async Task OnTrackStuck(TrackStuckEventArgs arg)
    {
        _logger.LogError($"Трек {arg.Track.Title} застакался на {arg.Threshold} мс. Чекай консоль на логи");
        arg.Player.Queue.Enqueue(arg.Track);
        await arg.Player.TextChannel.SendMessageAsync($"Трек {arg.Track.Title} был передобавлен в очередь после стака");
    }

    private Task OnWebSocketClosed(WebSocketClosedEventArgs arg)
    {
        _logger.LogCritical($"Дискорд Вебсокет коннект был разорван в связи с: {arg.Reason}");
        return Task.CompletedTask;
    }
}