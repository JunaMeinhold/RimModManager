namespace RimModManager.RimWorld.Profiles
{
    using Hexa.NET.D3DCommon;
    using System.Xml;

    public class RimProfile
    {
        private readonly List<string> activeModIds;
        private readonly List<string> knownExpansions;
        private readonly List<RimMod> activeMods;

        public RimProfile()
        {
            Name = string.Empty;
            Version = string.Empty;
            activeModIds = [];
            knownExpansions = [];
            activeMods = [];
        }

        public RimProfile(string name, string version, List<string> activeModIds, List<string> knownExpansions)
        {
            Name = name;
            Version = version;
            this.activeModIds = activeModIds;
            this.knownExpansions = knownExpansions;
            activeMods = [];
        }

        public RimProfile(string name, ModsConfig config)
        {
            Name = name;
            Version = config.Version;
            activeModIds = [.. config.ActiveModIds];
            knownExpansions = [.. config.KnownExpansions];
            activeMods = [];
        }

        public string Name { get; set; }

        public string Version { get; set; }

        public RimVersion RimVersion => RimVersion.Parse(Version);

        public IReadOnlyList<string> ActiveModIds => activeModIds;

        public IReadOnlyList<string> KnownExpansions => knownExpansions;

        public IReadOnlyList<RimMod> ActiveMods => activeMods;

        public RimMessageCollection Messages { get; } = [];

        public int WarningsCount => Messages.WarningsCount;

        public int ErrorsCount => Messages.ErrorsCount;

        public void PopulateList(RimModList mods)
        {
            activeMods.Clear();
            foreach (string modId in activeModIds)
            {
                if (!mods.TryGetMod(modId, out var mod))
                {
                    mod = RimMod.CreateUnknown(modId);
                    AddMessage(mod, RimSeverity.Error, "Missing mod detected.");
                }
                else
                {
                    mod = mod.Clone(); // deep clone to avoid problems with the main list.
                }

                activeMods.Add(mod);
            }

            CheckForProblems(mods);
        }

        public void CheckForProblems(RimModList mods)
        {
            ProblemChecker.CheckForProblems(Messages, activeMods, mods.PackageIdToMod, activeModIds, RimVersion, true);
        }

        private void AddMessage(RimMod mod, RimSeverity severity, string message)
        {
            RimMessage rimMessage = new(mod, message, severity);
            Messages.Add(rimMessage);
            mod.AddMessage(message, severity);
        }

        public static RimProfile Load(string path)
        {
            using XmlReader reader = XmlReader.Create(path);
            RimProfile profile = new();

            string currentElement = string.Empty;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "version")
                {
                    reader.Read();
                    profile.Version = reader.Value;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "activeMods")
                {
                    profile.activeModIds.AddRange(ParseList(reader, "activeMods"));
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "knownExpansions")
                {
                    profile.knownExpansions.AddRange(ParseList(reader, "knownExpansions"));
                }
            }

            return profile;
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

        public void Save(string path)
        {
            using XmlWriter writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true });

            writer.WriteStartDocument();
            writer.WriteStartElement("ModManagerProfile");

            writer.WriteElementString("version", Version);

            writer.WriteStartElement("activeMods");
            foreach (var mod in ActiveModIds)
            {
                writer.WriteStartElement("li");
                writer.WriteString(mod.ToLowerInvariant());
                writer.WriteEndElement(); // </li>
            }
            writer.WriteEndElement(); // </activeMods>

            writer.WriteStartElement("knownExpansions");
            foreach (var expansion in KnownExpansions)
            {
                writer.WriteStartElement("li");
                writer.WriteString(expansion);
                writer.WriteEndElement(); // </li>
            }
            writer.WriteEndElement(); // </knownExpansions>

            writer.WriteEndElement(); // </ModManagerProfile>
            writer.WriteEndDocument();
        }
    }
}