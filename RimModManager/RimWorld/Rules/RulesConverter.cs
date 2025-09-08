namespace RimModManager.RimWorld.Rules
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class RulesConverter : JsonConverter<Dictionary<string, Rule>>
    {
        public override Dictionary<string, Rule>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            Dictionary<string, Rule> dictionary = [];

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token");
                }

                string key = reader.GetString()!;
                reader.Read();

                var modState = JsonSerializer.Deserialize(ref reader, RuleSourceGenerationContext.Default.Rule)!;
                dictionary[key] = modState;
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, Rule> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, RuleSourceGenerationContext.Default.Rule);
            }

            writer.WriteEndObject();
        }
    }
}