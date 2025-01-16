namespace RimModManager.RimWorld.Rules
{
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RuleSet))]
    internal partial class RuleSetSourceGenerationContext : JsonSerializerContext
    {
    }
}