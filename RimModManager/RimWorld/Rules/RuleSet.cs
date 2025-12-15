namespace RimModManager.RimWorld.Rules
{
    using RimModManager.RimWorld;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class RuleSet
    {
        private ModReferenceSource referenceSource;

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("rules")]
        [JsonConverter(typeof(RulesConverter))]
        public Dictionary<string, Rule> Rules { get; set; } = [];

        public static RuleSet Load(string path, ModReferenceSource referenceSource)
        {
            if (!File.Exists(path)) return new();
            using var fs = File.OpenRead(path);
            var set = JsonSerializer.Deserialize(fs, RuleSetSourceGenerationContext.Default.RuleSet)!;
            set.referenceSource = referenceSource;
            return set;
        }

        public void Write(string path)
        {
            using var fs = File.Create(path);
            JsonSerializer.Serialize(fs, this, RuleSetSourceGenerationContext.Default.RuleSet);
        }

        private static RuleSet? communityRules;
        private static RuleSet? customRules;

        public static RuleSet CommunityRules
        {
            get
            {
                return communityRules ??= Load(DatabaseUpdater.CommunityRulesPath, ModReferenceSource.CommunityRules);
            }
        }

        public static RuleSet CustomRules
        {
            get
            {
                return customRules ??= Load(DatabaseUpdater.CustomRulesPath, ModReferenceSource.CustomRules);
            }
        }

        public static void ResetCommunityRules()
        {
            communityRules = null;
        }

        public static void ResetCustomRules()
        {
            customRules = null;
        }

        public IEnumerable<ModReference> EnumerateLoadBefore(RimMod mod, IReadOnlyDictionary<string, RimMod> packageIdToMod)
        {
            if (Rules.TryGetValue(mod.PackageId, out var rule))
            {
                foreach (var before in rule.LoadBefore)
                {
                    yield return ModReference.BuildRef(before.Key, packageIdToMod, ModReferenceDirection.LoadBefore, referenceSource, false);
                }
            }
        }

        public IEnumerable<ModReference> EnumerateLoadAfter(RimMod mod, IReadOnlyDictionary<string, RimMod> packageIdToMod)
        {
            if (Rules.TryGetValue(mod.PackageId, out var rule))
            {
                foreach (var before in rule.LoadAfter)
                {
                    yield return ModReference.BuildRef(before.Key, packageIdToMod, ModReferenceDirection.LoadAfter, referenceSource, false);
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