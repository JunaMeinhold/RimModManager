namespace RimModManager.RimWorld
{
    using System.Collections.Generic;
    using System.Xml;

    public class RimSaveGameMetadata
    {
        public RimVersion Version => RimVersion.Parse(GameVersion);

        public string GameVersion { get; set; } = string.Empty;

        public List<string> ModIds { get; set; } = [];

        public List<long> ModSteamIds { get; set; } = [];

        public List<string> ModNames { get; set; } = [];

        public void Read(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "meta")
                {
                    break;
                }

                if (reader.IsStartElement("gameVersion"))
                {
                    GameVersion = reader.ReadElementContentAsString();
                }

                if (reader.IsStartElement("modIds"))
                {
                    ReadList(reader, "modIds", ModIds);
                }

                if (reader.IsStartElement("modSteamIds"))
                {
                    ReadList(reader, "modSteamIds", ModSteamIds);
                }

                if (reader.IsStartElement("modNames"))
                {
                    ReadList(reader, "modNames", ModNames);
                }
            }
        }

        private static void ReadList(XmlReader reader, string tag, List<string> list)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == tag)
                {
                    break;
                }

                if (reader.IsStartElement("li"))
                {
                    list.Add(reader.ReadElementContentAsString());
                }
            }
        }

        private static void ReadList(XmlReader reader, string tag, List<long> list)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == tag)
                {
                    break;
                }

                if (reader.IsStartElement("li"))
                {
                    list.Add(reader.ReadElementContentAsLong());
                }
            }
        }
    }
}