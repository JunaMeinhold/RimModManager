namespace RimModManager.RimWorld.Rules
{
    using Newtonsoft.Json;
    using RimModManager.RimWorld;

    public class RuleSet
    {
        public int Timestamp { get; set; }

        public Dictionary<string, Rule> Rules { get; set; } = [];

        public static RuleSet Load(string path)
        {
            if (!File.Exists(path)) return new();
            using var fs = File.OpenRead(path);
            JsonTextReader reader = new(new StreamReader(fs));

            RuleSet set = new();
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var prop = (string?)reader.Value;

                    if (prop == "timestamp")
                    {
                        set.Timestamp = reader.ReadAsInt32() ?? 0;
                    }
                    else if (prop == "rules")
                    {
                        set.Rules = LoadRulesSection(reader);
                    }
                }
            }

            return set;
        }

        private static Dictionary<string, Rule> LoadRulesSection(JsonTextReader reader)
        {
            Dictionary<string, Rule> rules = new(StringComparer.OrdinalIgnoreCase);

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var ruleKey = (string?)reader.Value!;
                    var rule = new Rule();
                    rule.Read(reader);

                    rules[ruleKey] = rule;
                }
            }

            return rules;
        }

        public void Write(string path)
        {
            using var fs = File.Create(path);
        }

        private static RuleSet? communityRules;
        private static RuleSet? customRules;

        public static RuleSet CommunityRules
        {
            get
            {
                return communityRules ??= Load("database/communityRules.json");
            }
        }

        public static RuleSet CustomRules
        {
            get
            {
                return customRules ??= Load("database/customRules.json");
            }
        }

        public IEnumerable<ModReference> EnumerateLoadBefore(RimMod mod, IReadOnlyDictionary<string, RimMod> packageIdToMod)
        {
            if (Rules.TryGetValue(mod.PackageId, out var rule))
            {
                foreach (var before in rule.LoadBefore)
                {
                    yield return ModReference.BuildRef(before.Key, packageIdToMod, true, false, false);
                }
            }
        }

        public IEnumerable<ModReference> EnumerateLoadAfter(RimMod mod, IReadOnlyDictionary<string, RimMod> packageIdToMod)
        {
            if (Rules.TryGetValue(mod.PackageId, out var rule))
            {
                foreach (var before in rule.LoadAfter)
                {
                    yield return ModReference.BuildRef(before.Key, packageIdToMod, false, true, false);
                }
            }
        }

        public bool? LoadBottom(RimMod mod)
        {
            if (Rules.TryGetValue(mod.PackageId, out var rule))
            {
                return rule.LoadBottom?.Value;
            }

            return null;
        }
    }
}