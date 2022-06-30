using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Discord_Music_Bot.Core.Managers;

public sealed class AudioService
{
    private readonly LavaNode _lavaNode = ServiceManager.Provider.GetRequiredService<LavaNode>();

    public AudioService()
    {
        _lavaNode.OnTrackEnded += OnTrackEnded;
        _lavaNode.OnTrackStarted += OnTrackStarted;
        _lavaNode.OnTrackException += OnTrackException;
        _lavaNode.OnTrackStuck += OnTrackStuck;
    }
    
    private async Task OnTrackStarted(TrackStartEventArgs arg)
    {
        await arg.Player.TextChannel.SendMessageAsync($"Сейчас играет {arg.Track.Title}");
    }
   
    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (args.Reason != TrackEndReason.Finished) return;

        var player = args.Player;
        if (!player.Queue.TryDequeue(out var lavaTrack)) {
            await player.TextChannel.SendMessageAsync("Очередь за сосиками закончилась, купи подписку и покупай сосиски без очереди");
            /*_ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(10));*/
            return; 
        }

        if (lavaTrack is null)
        {
            await player.TextChannel.SendMessageAsync(
                "Следующий объект в очереди не является треком, так же, как твои родители не являются тебе родными");
            return;
        }

        await args.Player.PlayAsync(lavaTrack);
        await args.Player.TextChannel.SendMessageAsync(
            $"{args.Reason}: {args.Track.Title}\nСейчас ебашит: {lavaTrack.Title}");
    }
    
    private async Task OnTrackException(TrackExceptionEventArgs arg)
    {
        Console.WriteLine($"Трек {arg.Track.Title} вызвал ошибку, чекай логи");
        arg.Player.Queue.Enqueue(arg.Track);
        await arg.Player.TextChannel.SendMessageAsync($"{arg.Track.Title} передобавлен в очередь после вызова ошибки");
    }

    private async Task OnTrackStuck(TrackStuckEventArgs arg)
    {
        Console.WriteLine($"Трек {arg.Track.Title} застакался на {arg.Threshold} мс. Чекай консоль на логи");
        arg.Player.Queue.Enqueue(arg.Track);
        await arg.Player.TextChannel.SendMessageAsync($"Трек {arg.Track.Title} был передобавлен в очередь после стака");
    }
}