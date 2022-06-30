using System.ComponentModel;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace Discord_Music_Bot.Core.Managers;

public sealed class AudioManager : ModuleBase<SocketCommandContext>
{
    private readonly LavaNode _lavaNode;
    private readonly AudioService _audioService;
    private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);

    public AudioManager(LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
        _audioService = new AudioService(_lavaNode,new LoggerFactory());
    }

    [Command("Join")]
    [Alias("j")]
    public async Task JoinAsync()
    {
        if (_lavaNode.HasPlayer(Context.Guild))
        {
            await ReplyAsync("Я уже подключен к каналу еблан");
            return;
        }

        var voiceState = Context.User as IVoiceState;
        if (voiceState?.VoiceChannel == null)
        {
            await ReplyAsync("В канал зайди долдбаеб");
            return;
        }

        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            await ReplyAsync($"Подключился к кучке ебланов в канале: {voiceState.VoiceChannel.Name}!");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Leave")]
    [Alias("l")]
    public async Task LeaveAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        var voiceChannel = (Context.User as IVoiceState)?.VoiceChannel ?? player.VoiceChannel;
        if (voiceChannel == null)
        {
            await ReplyAsync("Я слишком умный, чтобы понять, от какого канала ты хочешь, чтобы я отлкючисля");
            return;
        }

        try
        {
            await _lavaNode.LeaveAsync(voiceChannel);
            await ReplyAsync($"Заебало, я сваливаю от уебков в канале: {voiceChannel.Name}!");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Play")]
    [Alias("p")]
    public async Task PlayAsync([Remainder] string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await ReplyAsync("Долбаеб, я по твоему воздух искать должен?");
            return;
        }

        if (!_lavaNode.HasPlayer(Context.Guild))
        {
            await JoinAsync();
        }

        var searchResponse = Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute)
            ? await _lavaNode.SearchAsync(SearchType.Direct, searchQuery)
            : await _lavaNode.SearchYouTubeAsync(searchQuery);
        if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
        {
            await ReplyAsync($"Я нихуя не нашел по теме: `{searchQuery}`.");
            return;
        }

        var player = _lavaNode.GetPlayer(Context.Guild);
        if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
        {
            player.Queue.Enqueue(searchResponse.Tracks);
            await ReplyAsync($"Добавил в очередь на избиение эти песни: {searchResponse.Tracks.Count}");
        }
        else
        {
            var track = searchResponse.Tracks.FirstOrDefault();
            player.Queue.Enqueue(track);

            await ReplyAsync($"Добвил в очередбь на избиение эту песню: {track?.Title}");
        }

        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            return;
        }

        player.Queue.TryDequeue(out var lavaTrack);
        await player.PlayAsync(x =>
        {
            x.Track = lavaTrack;
            x.ShouldPause = false;
        });
    }

    [Command("Pause")]
    [Alias("ps")]
    public async Task PauseAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await ReplyAsync("Я могу поставить на паузу только твое развитие долбаеб");
            return;
        }

        try
        {
            await player.PauseAsync();
            await ReplyAsync($"На паузе: {player.Track.Title}");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Resume")]
    [Alias("rs")]
    public async Task ResumeAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        if (player.PlayerState != PlayerState.Paused)
        {
            await ReplyAsync("Хоть я и поставил на паузу твое развитие, но продолжить его не сможет даже бог");
            return;
        }

        try
        {
            await player.ResumeAsync();
            await ReplyAsync($"Продолжено с паузы: {player.Track.Title}");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Stop")]
    [Alias("sp")]
    public async Task StopAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        if (player.PlayerState == PlayerState.Stopped)
        {
            await ReplyAsync("Уебище, я щас не просто поставлю на паузу твое развитие, я его сведу на ноль нахуй");
            return;
        }

        try
        {
            await player.StopAsync();
            await ReplyAsync("Для таких уебков больше не произвожу ничего");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Skip")]
    [Alias("s")]
    public async Task SkipAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await ReplyAsync("Я ща скипну твое очко на мой киберхуй гнида");
            return;
        }

        try
        {
            var (oldTrack, currenTrack) = await player.SkipAsync();
            await ReplyAsync($"Пропущено: {oldTrack.Title}\nИграет: {currenTrack.Title}");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Seek")]
    public async Task SeekAsync(TimeSpan timeSpan)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await ReplyAsync("Я заебался, отвали");
            return;
        }

        try
        {
            await player.SeekAsync(timeSpan);
            await ReplyAsync($"Еще какая то хуйня про трек `{player.Track.Title}`-{timeSpan}.");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Volume")]
    public async Task VolumeAsync(ushort volume)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        try
        {
            await player.UpdateVolumeAsync(volume);
            await ReplyAsync($"Громкость изменена на {volume}.");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("NowPlaying"), Alias("Np")]
    public async Task NowPlayingAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await ReplyAsync("Как я нахуй должен тебе показать, что играет, если нихуя не играет?");
            return;
        }

        var track = player.Track;
        var artwork = await track.FetchArtworkAsync();

        var embed = new EmbedBuilder()
            .WithAuthor(track.Author, Context.Client.CurrentUser.GetAvatarUrl(), track.Url)
            .WithTitle($"Сейчас играет: {track.Title}")
            .WithImageUrl(artwork)
            .WithFooter($"{track.Position}/{track.Duration}");

        await ReplyAsync(embed: embed.Build());
    }

    [Command("Genius", RunMode = RunMode.Async)]
    public async Task ShowGeniusLyrics()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await ReplyAsync("Как я блядь по твоему должен искать текст песни, которой нет? Как шазам из головы взять?");
            return;
        }

        var lyrics = await player.Track.FetchLyricsFromGeniusAsync();
        if (string.IsNullOrWhiteSpace(lyrics))
        {
            await ReplyAsync($"Нету текста братик по треку: {player.Track.Title}");
            return;
        }

        await SendLyricsAsync(lyrics);
    }

    [Command("OVH", RunMode = RunMode.Async)]
    public async Task ShowOvhLyrics()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await ReplyAsync("Я не подключен ни к одному каналу долбаеб");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await ReplyAsync("Как я блядь по твоему должен искать текст песни, которой нет? Как шазам из головы взять?");
            return;
        }

        var lyrics = await player.Track.FetchLyricsFromOvhAsync();
        if (string.IsNullOrWhiteSpace(lyrics))
        {
            await ReplyAsync($"Нету текста братик по треку: {player.Track.Title}");
            return;
        }

        await SendLyricsAsync(lyrics);
    }

    [Command("Queue")]
    public Task QueueAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            return ReplyAsync("Я не подключен ни к одному каналу долбаеб");
        }

        return ReplyAsync(player.PlayerState != PlayerState.Playing
            ? "Как я назуй должен показать тебе очередь, если там ничего нет? Ты меня сейчас доведешь, я поставлю 10 часов Летова на громкости 300 тебе в уши блять"
            : string.Join(Environment.NewLine, player.Queue.Select(x => x.Title)));
    }

    private async Task SendLyricsAsync(string lyrics)
    {
        var splitLyrics = lyrics.Split(Environment.NewLine);
        var stringBuilder = new StringBuilder();
        foreach (var line in splitLyrics)
        {
            if (line.Contains('['))
            {
                stringBuilder.Append(Environment.NewLine);
            }

            if (Range.Contains(stringBuilder.Length))
            {
                await ReplyAsync($"```{stringBuilder}```");
                stringBuilder.Clear();
            }
            else
            {
                stringBuilder.AppendLine(line);
            }
        }
        

        await ReplyAsync($"```{stringBuilder}```");
    }
}
