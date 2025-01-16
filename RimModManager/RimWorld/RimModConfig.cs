namespace RimModManager.RimWorld
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class RimModManagerConfig
    {
        public string? GameFolder { get; set; }

        public string? GameConfigFolder { get; set; }

        public string? SteamModFolder { get; set; }

        public static RimModManagerConfig Default { get; } = Load("config.json");

        public static RimModManagerConfig Load(string path)
        {
            if (!File.Exists(path)) return new();
            using var fs = File.OpenRead(path);
            RimModManagerConfig config = (RimModManagerConfig?)JsonSerializer.Deserialize(fs, typeof(RimModManagerConfig), RimModManagerConfigGenerationContext.Default) ?? new();
            return config;
        }

        public void Write(string path)
        {
            using var fs = File.Create(path);
            JsonSerializer.Serialize(fs, this, typeof(RimModManagerConfig), RimModManagerConfigGenerationContext.Default);
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RimModManagerConfig))]
    internal partial class RimModManagerConfigGenerationContext : JsonSerializerContext
    {
    }
}