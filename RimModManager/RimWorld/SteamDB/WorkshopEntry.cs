namespace RimModManager.RimWorld.SteamDB
{
    using Newtonsoft.Json;

    public class WorkshopEntry
    {
        public string Url { get; set; } = null!;

        public string PackageId { get; set; } = null!;

        public List<string> GameVersions { get; set; } = [];

        public string SteamName { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Authors { get; set; } = null!;

        public Dictionary<long, Dependency> Dependencies { get; set; } = [];

        public void Read(JsonReader reader)
        {
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = (string)reader.Value!;
                    switch (propertyName)
                    {
                        case "url":
                            Url = reader.ReadAsString()!;
                            break;

                        case "packageId":
                            PackageId = reader.ReadAsString()!;
                            break;

                        case "gameVersions":
                            GameVersions = ReadStringList(reader);
                            break;

                        case "steamName":
                            SteamName = reader.ReadAsString()!;
                            break;

                        case "name":
                            Name = reader.ReadAsString()!;
                            break;

                        case "authors":
                            Authors = reader.ReadAsString()!;
                            break;

                        case "dependencies":
                            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                            {
                                if (reader.TokenType == JsonToken.PropertyName)
                                {
                                    string dependencyIdString = (string)reader.Value!;
                                    long dependencyId = long.Parse(dependencyIdString);
                                    Dependency dependency = new();
                                    dependency.Read(reader);
                                    Dependencies[dependencyId] = dependency;
                                }
                            }
                            break;
                    }
                }
            }
        }

        private static List<string> ReadStringList(JsonReader reader)
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