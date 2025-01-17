namespace RimModManager.RimWorld
{
    using System;
    using System.Text.Json.Serialization;

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(RimModManagerConfig))]
    internal partial class RimModManagerConfigGenerationContext : JsonSerializerContext
    {
    }
}