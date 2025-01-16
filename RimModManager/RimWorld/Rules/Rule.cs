namespace RimModManager.RimWorld.Rules
{
    using Newtonsoft.Json;

    public class Rule
    {
        public Dictionary<string, RuleDetails> LoadBefore { get; set; } = [];

        public Dictionary<string, RuleDetails> LoadAfter { get; set; } = [];

        public LoadBottom? LoadBottom { get; set; }

        public void Read(JsonTextReader reader)
        {
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var prop = (string?)reader.Value;

                    if (prop == "loadBefore")
                    {
                        LoadBefore = LoadSection(reader);
                    }
                    else if (prop == "loadAfter")
                    {
                        LoadAfter = LoadSection(reader);
                    }
                    else if (prop == "loadBottom")
                    {
                        LoadBottom = new();
                        LoadBottom.Read(reader);
                    }
                }
            }
        }

        private static Dictionary<string, RuleDetails> LoadSection(JsonTextReader reader)
        {
            Dictionary<string, RuleDetails> section = new(StringComparer.OrdinalIgnoreCase);

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var modKey = (string?)reader.Value!;

                    var ruleDetails = new RuleDetails();
                    ruleDetails.Read(reader);

                    section[modKey] = ruleDetails;
                }
            }

            return section;
        }
    }

    public class LoadBottom
    {
        public bool Value { get; set; }

        public void Read(JsonTextReader reader)
        {
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var prop = (string?)reader.Value;

                    if (prop == "value")
                    {
                        Value = reader.ReadAsBoolean() ?? false;
                    }
                }
            }
        }
    }
}