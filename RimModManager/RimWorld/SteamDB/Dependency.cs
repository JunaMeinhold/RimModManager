namespace RimModManager.RimWorld.SteamDB
{
    using Newtonsoft.Json;

    public struct Dependency
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public void Read(JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType == JsonToken.StartArray)
            {
                Name = reader.ReadAsString()!;
                Url = reader.ReadAsString()!;
            }
        }
    }
}