namespace RimModManager.RimWorld.Rules
{
    using System.Text.Json.Serialization;

    public struct LoadBottom
    {
        [JsonPropertyName("value")]
        public bool Value { get; set; }
    }
}