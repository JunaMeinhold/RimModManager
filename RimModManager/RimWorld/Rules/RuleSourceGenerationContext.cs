namespace RimModManager.RimWorld.Rules
{
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Rule))]
    internal partial class RuleSourceGenerationContext : JsonSerializerContext
    {
    }
}