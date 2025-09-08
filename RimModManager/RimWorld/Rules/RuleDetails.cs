namespace RimModManager.RimWorld.Rules
{
    using Hexa.NET.KittyUI.Debugging;
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class RuleDetails
    {
        [JsonPropertyName("name")]
        [JsonConverter(typeof(TextConverter))]
        public List<string> Name { get; set; } = [];

        [JsonPropertyName("comment")]
        [JsonConverter(typeof(TextConverter))]
        public List<string> Comment { get; set; } = [];
    }

    public class TextConverter : JsonConverter<List<string>>
    {
        public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            List<string> texts = [];

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    texts.Add(reader.GetString()!);
                    break;

                case JsonTokenType.StartArray:
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            return texts;
                        }

                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new JsonException("Expected String token");
                        }

                        texts.Add(reader.GetString()!);
                    }
                    break;
            }

            return texts;
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            for (int i = 0; i < value.Count; i++)
            {
                writer.WriteStringValue(value[i]);
            }

            writer.WriteEndArray();
        }
    }
}