using Newtonsoft.Json;

namespace Discord_Music_Bot.Core.Managers;

public static class ConfigManager
{
    private static string configFolder = "Resources";
    private static string configFile = "config.json";
    private static string configPath = configFolder + "/" + configFile;
    public static BotConfig Config { get; private set; }

    static ConfigManager()
    {
        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        if (!File.Exists(configPath))
        {
            Config = new BotConfig();
            var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
            File.WriteAllText(configPath, json);
        }
        else
        {
            var json = File.ReadAllText(configPath);
            Config = JsonConvert.DeserializeObject<BotConfig>(json);
        }
            
    }
}

public struct BotConfig
{
    [JsonProperty("token")]
    public string token { get; private set; }
    [JsonProperty("prefix")]
    public string prefix { get; private set; }
}