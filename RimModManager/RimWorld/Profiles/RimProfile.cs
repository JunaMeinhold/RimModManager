namespace RimModManager.RimWorld.Profiles
{
    using Hexa.NET.ImGui;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.Utilities.Text;
    using System.Numerics;
    using System.Xml;

    public class RimProfile
    {
        private readonly List<string> activeModOrder;
        private readonly List<string> knownExpansions;
        private readonly List<RimMod> activeMods;
        private readonly HashSet<string> activeModIds = new(StringComparer.OrdinalIgnoreCase);

        public RimProfile()
        {
            Name = string.Empty;
            Version = string.Empty;
            activeModOrder = [];
            knownExpansions = [];
            activeMods = [];
        }

        public RimProfile(string name, string version, List<string> activeModIds, List<string> knownExpansions)
        {
            Name = name;
            Version = version;
            this.activeModOrder = activeModIds;
            this.knownExpansions = knownExpansions;
            activeMods = [];
        }

        public RimProfile(string name, RimLoadOrder config)
        {
            Name = name;
            Version = config.Version;
            activeModOrder = [.. config.ActiveModsOrder];
            knownExpansions = [.. config.KnownExpansions];
            activeMods = [];
        }

        public string Name { get; set; }

        public string Version { get; set; }

        public RimVersion RimVersion => RimVersion.Parse(Version);

        public IReadOnlyList<string> ActiveModOrder => activeModOrder;

        public IReadOnlySet<string> ActiveModIds => activeModIds;

        public IReadOnlyList<string> KnownExpansions => knownExpansions;

        public IReadOnlyList<RimMod> ActiveMods => activeMods;

        public RimMessageCollection Messages { get; } = new() { HideInactiveModMessages = true, };

        public int WarningsCount => Messages.WarningsCount;

        public int ErrorsCount => Messages.ErrorsCount;

        public void PopulateList(RimModList mods)
        {
            var clonedList = mods.Clone();
            activeMods.Clear();
            foreach (string modId in activeModOrder)
            {
                if (!clonedList.TryGetMod(modId, out var mod))
                {
                    mod = RimMod.CreateUnknown(modId);
                    AddMessage(mod, RimSeverity.Error, "Missing mod detected.");
                }

                mod.IsActive = true;

                activeModIds.Add(modId);
                activeMods.Add(mod);
            }

            foreach (var mod in clonedList)
            {
                if (!ActiveModIds.Contains(mod.PackageId))
                {
                    mod.IsActive = false;
                }
            }

            CheckForProblems(clonedList);
        }

        public void CheckForProblems(RimModList mods)
        {
            ProblemChecker.CheckForProblems(Messages, this, mods, RimVersion, true);
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
                    profile.activeModOrder.AddRange(ParseList(reader, "activeMods"));
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
            foreach (var mod in ActiveModOrder)
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

        public unsafe void DrawLoadOrder(StrBuilder builder)
        {
            ImGuiManager.PushFont("FA");

            ImGui.Text("Name:"u8);
            ImGui.SameLine();
            ImGui.Text(Name);
            ImGui.Text("Game Version:"u8);
            ImGui.SameLine();
            ImGui.Text(Version);
            ImGui.Separator();

            float lineHeight = ImGui.GetTextLineHeightWithSpacing();

            if (ImGui.BeginTable("##Mods"u8, 2, ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable, new Vector2(0, -lineHeight)))
            {
                ImGui.TableSetupColumn("Index"u8);
                ImGui.TableSetupColumn("Name"u8);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                for (int i = 0; i < ActiveMods.Count; i++)
                {
                    RimMod mod = ActiveMods[i];
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    builder.Reset();
                    builder.Append(i);
                    builder.End();
                    ImGui.Text(builder);

                    ImGui.TableSetColumnIndex(1);
                    var width = ImGui.GetContentRegionAvail().X;
                    builder.Reset();
                    builder.Append(mod.GetIcon());
                    builder.End();
                    ImGui.TextColored(mod.GetIconColor(), builder);

                    ImGui.SameLine();

                    ImGui.Selectable(mod.Name);

                    bool hovered = ImGui.IsItemHovered();

                    bool messagesHovered = mod.DrawMessages(builder, hovered, width);

                    if (hovered && !messagesHovered)
                    {
                        mod.DrawTooltip(builder);
                    }
                }

                ImGui.EndTable();
            }

            Messages.DrawBar(builder);

            ImGuiManager.PopFont();
        }
    }
}