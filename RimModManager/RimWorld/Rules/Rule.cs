namespace RimModManager.RimWorld.Rules
{
    using System.Text.Json.Serialization;

    public class Rule
    {
        [JsonPropertyName("loadBefore")]
        public Dictionary<string, RuleDetails> LoadBefore { get; set; } = [];

        [JsonPropertyName("loadAfter")]
        public Dictionary<string, RuleDetails> LoadAfter { get; set; } = [];

        [JsonPropertyName("loadBottom")]
        public LoadBottom? LoadBottom { get; set; }
    }
}