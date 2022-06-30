using Discord_Music_Bot.Core;

namespace Discord_Music_Bot;

public class Program
{
    static void Main(string[] args)
        => new Bot().MainAsync().GetAwaiter().GetResult();
}