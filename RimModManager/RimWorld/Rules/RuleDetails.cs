namespace RimModManager.RimWorld.Rules
{
    using Newtonsoft.Json;

    public class RuleDetails
    {
        public List<string> Name { get; set; } = [];

        public List<string> Comment { get; set; } = [];

        public void Read(JsonTextReader reader)
        {
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var prop = (string?)reader.Value;

                    if (prop == "name")
                    {
                        Name = ReadStringList(reader);
                    }
                    else if (prop == "comment")
                    {
                        Comment = ReadStringList(reader);
                    }
                }
            }
        }

        private static List<string> ReadStringList(JsonTextReader reader)
        {
            var list = new List<string>();

            if (reader.Read() && reader.TokenType == JsonToken.StartArray)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    if (reader.TokenType == JsonToken.String)
                    {
                        list.Add((string?)reader.Value ?? string.Empty);
                    }
                }
            }

            return list;
        }
    }
}