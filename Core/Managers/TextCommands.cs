using Discord.Commands;

namespace Discord_Music_Bot.Core.Managers;

public class TextCommands : ModuleBase<SocketCommandContext>
{
    [Command("бебра")]
    [Alias("b")]
    [Summary("Ну бебра, как бебра, че бубнить то")]
    public Task SayBebraAsync() =>
        ReplyAsync($"{Context.Message.Author.Mention} сам ты беб... бебр... иди нахуй короче");

    [Command("стена")]
    [Summary("Стену пили долбаеб")]
    public Task SayStenaAsync() =>
        ReplyAsync($"{Context.Message.Author.Mention} стену пили долбаеб");
}