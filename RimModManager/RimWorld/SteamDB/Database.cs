namespace RimModManager.RimWorld.SteamDB
{
    using Newtonsoft.Json;

    public class SteamDatabase
    {
        public long Version;

        public Dictionary<long, WorkshopEntry> Entries { get; set; } = [];

        public IEnumerable<ModReference> EnumerateDependencies(RimMod mod, IReadOnlyDictionary<long, RimMod> steamIdToMod)
        {
            if (mod.SteamId == null) yield break;

            if (Entries.TryGetValue(mod.SteamId.Value, out var entry))
            {
                foreach (var dependency in entry.Dependencies)
                {
                    yield return ModReference.BuildRef(dependency.Key, steamIdToMod, ModReferenceDirection.LoadAfter, true);
                }
            }
        }

        private static SteamDatabase? instance;

        public static SteamDatabase Instance
        {
            get => instance ??= Load("database/db.json");
        }

        public static SteamDatabase Load(string path)
        {
            if (!File.Exists(path)) return new();
            using var fs = File.OpenRead(path);
            JsonTextReader reader = new(new StreamReader(fs));

            SteamDatabase db = new();
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var prop = (string?)reader.Value;

                    if (prop == "timestamp")
                    {
                        db.Version = reader.ReadAsInt32() ?? 0;
                    }
                    else if (prop == "database")
                    {
                        db.Entries = LoadEntriesSection(reader);
                    }
                }
            }

            return db;
        }

        private static Dictionary<long, WorkshopEntry> LoadEntriesSection(JsonTextReader reader)
        {
            Dictionary<long, WorkshopEntry> rules = [];

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var ruleKey = long.Parse((string?)reader.Value!);
                    var rule = new WorkshopEntry();
                    rule.Read(reader);

                    rules[ruleKey] = rule;
                }
            }

            return rules;
        }
    }
}