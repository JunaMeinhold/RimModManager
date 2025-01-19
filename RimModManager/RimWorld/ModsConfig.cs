namespace RimModManager.RimWorld
{
    using System.Collections.Generic;
    using System.Xml;

    public readonly struct AZNameComparer : IComparer<RimMod>
    {
        public static readonly AZNameComparer Instance = new();

        public readonly int Compare(RimMod? x, RimMod? y)
        {
            if (x == null || y == null) return 0;

            return x.Name.CompareTo(y.Name);
        }
    }

    public class ModsConfig
    {
        public static RimLoadOrder Load(RimModManagerConfig config, RimModList modList)
        {
            if (!Directory.Exists(config.GameConfigFolder)) throw new InvalidOperationException("The base path to ModsConfig.xml doesn't exist.");

            return Load(Path.Combine(config.GameConfigFolder, "ModsConfig.xml"), modList);
        }

        public static RimLoadOrder Load(string path, RimModList modList)
        {
            using XmlReader reader = XmlReader.Create(path);
            RimLoadOrder configData = new(modList);

            string currentElement = string.Empty;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "version")
                {
                    reader.Read();
                    configData.Version = reader.Value;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "activeMods")
                {
                    configData.ActivateMods(ParseList(reader, "activeMods"));
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "knownExpansions")
                {
                    configData.KnownExpansions.AddRange(ParseList(reader, "knownExpansions"));
                }
            }

            configData.PopulateList(modList);

            return configData;
        }

        private static IEnumerable<string> ParseList(XmlReader reader, string elementName)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "li")
                {
                    reader.Read();
                    yield return reader.Value;
                }

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == elementName)
                {
                    yield break;
                }
            }
        }

        public static void Save(RimModManagerConfig config, RimLoadOrder loadOrder)
        {
            Save(Path.Combine(config.GameConfigFolder!, "ModsConfig.xml"), loadOrder);
        }

        public static void Save(string path, RimLoadOrder loadOrder)
        {
            using XmlWriter writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true });

            writer.WriteStartDocument();
            writer.WriteStartElement("ModsConfigData");

            writer.WriteElementString("version", loadOrder.Version);

            writer.WriteStartElement("activeMods");
            foreach (var mod in loadOrder.ActiveModsOrder)
            {
                writer.WriteStartElement("li");
                writer.WriteString(mod.ToLowerInvariant());
                writer.WriteEndElement(); // </li>
            }
            writer.WriteEndElement(); // </activeMods>

            writer.WriteStartElement("knownExpansions");
            foreach (var expansion in loadOrder.KnownExpansions)
            {
                writer.WriteStartElement("li");
                writer.WriteString(expansion);
                writer.WriteEndElement(); // </li>
            }
            writer.WriteEndElement(); // </knownExpansions>

            writer.WriteEndElement(); // </ModsConfigData>
            writer.WriteEndDocument();
        }
    }
}