namespace RimModManager.RimWorld
{
    using System.Collections.Generic;
    using System.Xml;

    public class RimSaveGame
    {
        public List<string> ModIds { get; set; } = [];

        public static RimSaveGame Load(string path)
        {
            using var fs = File.OpenRead(path);
            using XmlReader reader = XmlReader.Create(fs);
            RimSaveGame saveGame = new();

            while (reader.Read())
            {
                if (reader.IsStartElement("modIds"))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "modIds")
                        {
                            break;
                        }

                        if (reader.IsStartElement("li"))
                        {
                            saveGame.ModIds.Add(reader.ReadElementContentAsString());
                        }
                    }
                    break;
                }
            }

            return saveGame;
        }
    }
}