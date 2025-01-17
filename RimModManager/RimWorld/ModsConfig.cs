namespace RimModManager.RimWorld
{
    using Hexa.NET.SDL2;
    using RimModManager;
    using RimModManager.RimWorld.Rules;
    using RimModManager.RimWorld.Sorting;
    using RimModManager.RimWorld.SteamDB;
    using System.Collections.Generic;
    using System.Xml;

    public class ModsConfig
    {
        private readonly List<string> activeModIds;
        private readonly List<RimMod> activeMods;
        private readonly List<string> knownExpansions;
        private readonly List<RimMod> inactiveMods;

        public ModsConfig()
        {
            Version = string.Empty;
            activeModIds = [];
            knownExpansions = [];
            activeMods = [];
            inactiveMods = [];
        }

        public ModsConfig(string version, List<string> activeMods, List<string> knownExpansions)
        {
            Version = version;
            activeModIds = activeMods;
            this.knownExpansions = knownExpansions;
            this.activeMods = [];
            inactiveMods = [];
        }

        public string Version { get; set; }

        public RimVersion RimVersion => RimVersion.Parse(Version);

        public IReadOnlyList<string> ActiveModIds => activeModIds;

        public IReadOnlyList<RimMod> ActiveMods => activeMods;

        public IReadOnlyList<RimMod> InactiveMods => inactiveMods;

        public IReadOnlyList<string> KnownExpansions => knownExpansions;

        public List<RimMessage> Messages = [];

        public int WarningsCount;

        public int ErrorsCount;

        public void CheckForProblems()
        {
            ClearMessages();
            Dictionary<string, RimMod> packageIdToMod = new(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in activeMods)
            {
                packageIdToMod[mod.PackageId] = mod;
            }
            foreach (var mod in inactiveMods)
            {
                packageIdToMod[mod.PackageId] = mod;
            }

            HashSet<string> activeModsIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (var id in activeModIds)
            {
                activeModsIds.Add(id);
            }

            var currentVersion = RimVersion.ToCompareVersion();
            foreach (var mod in activeMods)
            {
                mod.ClearMessages();
                CheckGameVersion(currentVersion, mod);
                CheckDependencies(currentVersion, activeModsIds, mod);
                CheckIncompatibleMods(currentVersion, activeModsIds, packageIdToMod, mod);
                CheckLoadOrder(activeMods, mod);
            }
        }

        private void ClearMessages()
        {
            Messages.Clear();
            WarningsCount = 0;
            ErrorsCount = 0;
        }

        private void CheckGameVersion(RimVersion currentVersion, RimMod mod)
        {
            if (mod.Metadata.SupportedVersions.Count == 0) return;
            bool supported = false;
            foreach (var supportedVersion in mod.Metadata.SupportedVersions)
            {
                if (supportedVersion == currentVersion)
                {
                    supported = true;
                }
            }

            if (!supported)
            {
                AddMessage(mod, "Incompatible game version.", RimSeverity.Warn);
            }
        }

        private void CheckDependencies(RimVersion version, HashSet<string> activeModsIds, RimMod mod)
        {
            foreach (var deps in mod.Metadata.EnumerateDependencies(version))
            {
                if (!activeModsIds.Contains(deps.PackageId))
                {
                    AddMessage(mod, $"Mod depends on {deps.DisplayName} ({deps.PackageId}) but it's not loaded", RimSeverity.Error);
                }
            }
        }

        private void CheckIncompatibleMods(RimVersion version, HashSet<string> activeModsIds, Dictionary<string, RimMod> packageIdToMod, RimMod mod)
        {
            foreach (var incompatibleId in mod.Metadata.EnumerateIncompatibleWith(version))
            {
                if (activeModsIds.Contains(incompatibleId))
                {
                    AddMessage(mod, $"Mod is incompatible with {packageIdToMod[incompatibleId].Name}", RimSeverity.Error);
                }
            }
        }

        private void CheckLoadOrder(List<RimMod> activeMods, RimMod mod)
        {
            int index = activeMods.IndexOf(mod);
            foreach (var reference in mod.LoadBefore)
            {
                int otherIndex = activeMods.IndexOf(reference.Mod);
                if (otherIndex == -1) continue;
                if (index > otherIndex)
                {
                    if (reference.Forced)
                    {
                        AddMessage(mod, $"Mod must load before {reference.Mod.Name} ({reference.Mod.PackageId})", RimSeverity.Error);
                    }
                    else
                    {
                        AddMessage(mod, $"Mod should load before {reference.Mod.Name} ({reference.Mod.PackageId})", RimSeverity.Warn);
                    }
                }
            }

            foreach (var reference in mod.LoadAfter)
            {
                int otherIndex = activeMods.IndexOf(reference.Mod);
                if (otherIndex == -1) continue;
                if (index < otherIndex)
                {
                    if (reference.Forced)
                    {
                        AddMessage(mod, $"Mod must load after {reference.Mod.Name} ({reference.Mod.PackageId})", RimSeverity.Error);
                    }
                    else
                    {
                        AddMessage(mod, $"Mod should load after {reference.Mod.Name} ({reference.Mod.PackageId})", RimSeverity.Warn);
                    }
                }
            }
        }

        public void AddMessage(RimMod mod, string message, RimSeverity severity)
        {
            if (severity == RimSeverity.Warn) WarningsCount++;
            if (severity == RimSeverity.Error) ErrorsCount++;
            RimMessage msg = new(mod, message, severity);
            Messages.Add(msg);
            mod.AddMessage(message, severity);
        }

        public void ActivateMod(RimMod mod)
        {
            if (mod.IsActive) return;
            activeModIds.Add(mod.PackageId);
            activeMods.Add(mod);
            inactiveMods.Remove(mod);
            mod.IsActive = true;

            CheckForProblems();
        }

        public void DeactiveMod(RimMod mod)
        {
            if (!mod.IsActive) return;
            RemoveId(mod.PackageId);
            activeMods.Remove(mod);
            inactiveMods.Add(mod);
            mod.IsActive = false;

            CheckForProblems();
        }

        private void RemoveId(string id)
        {
            for (int i = 0; i < activeModIds.Count; i++)
            {
                string dd = activeModIds[i];
                if (StringComparer.OrdinalIgnoreCase.Compare(dd, id) == 0)
                {
                    activeModIds.RemoveAt(i);
                    return;
                }
            }
        }

        public void Clear()
        {
            for (int i = activeMods.Count - 1; i >= 0; i--)
            {
                var mod = activeMods[i];

                if (mod.Kind == ModKind.Base)
                {
                    continue;
                }
                activeMods.RemoveAt(i);
                activeModIds.RemoveAt(i);
                inactiveMods.Add(mod);
                mod.IsActive = false;
            }
            CheckForProblems();
        }

        public void ClearFull()
        {
            inactiveMods.AddRange(activeMods);
            activeMods.Clear();
            foreach (RimMod mod in activeMods)
            {
                mod.IsActive = false;
            }
            activeModIds.Clear();
            CheckForProblems();
        }

        public void Move(RimMod mod, int index)
        {
            if (mod.IsActive)
            {
                int oldIndex = activeMods.IndexOf(mod);
                if (oldIndex == -1) throw new ArgumentException("Item was not found in list.", nameof(mod));
                activeMods.RemoveAt(oldIndex);
                activeModIds.RemoveAt(oldIndex);

                activeMods.Insert(index, mod);
                activeModIds.Insert(index, mod.PackageId);
                CheckForProblems();
            }
        }

        private void PopulateList(RimModList modList)
        {
            inactiveMods.Clear();
            activeMods.Clear();
            foreach (string modId in ActiveModIds)
            {
                if (modList.TryGetMod(modId, out var mod))
                {
                    activeMods.Add(mod);
                }
                else
                {
                    mod = RimMod.CreateUnknown(modId);
                    activeMods.Add(mod);
                }

                mod.IsActive = true;
            }

            HashSet<string> activeModsIds = new(activeModIds, StringComparer.OrdinalIgnoreCase);

            foreach (var mod in modList)
            {
                if (!activeModsIds.Contains(mod.PackageId))
                {
                    inactiveMods.Add(mod);
                    mod.IsActive = false;
                }
            }

            var currentVersion = RimVersion.ToCompareVersion();
            ResolveModsDeps(currentVersion, modList, modList.PackageIdToMod, modList.SteamIdToMod);
        }

        private static void ResolveModsDeps(RimVersion currentVersion, IEnumerable<RimMod> allMods, IReadOnlyDictionary<string, RimMod> packageIdToMod, IReadOnlyDictionary<long, RimMod> steamIdToMod)
        {
            var communityRules = RuleSet.CommunityRules;
            var customRules = RuleSet.CustomRules;
            foreach (var mod in allMods)
            {
                mod.LoadAfter.Clear();
                mod.LoadBefore.Clear();

                mod.LoadAfter.AddRange(mod.Metadata.EnumerateDependenciesAsRef(currentVersion, packageIdToMod));
                mod.LoadAfter.AddRange(SteamDatabase.Instance.EnumerateDependencies(mod, steamIdToMod));
                mod.LoadBefore.AddRange(mod.Metadata.EnumerateLoadBefore(currentVersion, packageIdToMod));
                mod.LoadAfter.AddRange(mod.Metadata.EnumerateLoadAfter(currentVersion, packageIdToMod));

                mod.LoadBefore.AddRange(communityRules.EnumerateLoadBefore(mod, packageIdToMod));
                mod.LoadAfter.AddRange(communityRules.EnumerateLoadAfter(mod, packageIdToMod));

                mod.LoadBefore.AddRange(customRules.EnumerateLoadBefore(mod, packageIdToMod));
                mod.LoadAfter.AddRange(customRules.EnumerateLoadAfter(mod, packageIdToMod));

                mod.LoadBottom = customRules.LoadBottom(mod) ?? communityRules.LoadBottom(mod);
            }
        }

        public static ModsConfig Load(RimModManagerConfig config, RimModList modList)
        {
            if (!Directory.Exists(config.GameConfigFolder)) throw new InvalidOperationException("The base path to ModsConfig.xml doesn't exist.");

            return Load(Path.Combine(config.GameConfigFolder, "ModsConfig.xml"), modList);
        }

        public static ModsConfig Load(string path, RimModList modList)
        {
            using XmlReader reader = XmlReader.Create(path);
            ModsConfig configData = new();

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
                    configData.activeModIds.AddRange(ParseList(reader, "activeMods"));
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "knownExpansions")
                {
                    configData.knownExpansions.AddRange(ParseList(reader, "knownExpansions"));
                }
            }

            configData.PopulateList(modList);
            configData.inactiveMods.Sort(AZNameComparer.Instance);

            return configData;
        }

        private readonly struct AZNameComparer : IComparer<RimMod>
        {
            public static AZNameComparer Instance = new();

            public readonly int Compare(RimMod? x, RimMod? y)
            {
                if (x == null || y == null) return 0;

                return x.Name.CompareTo(y.Name);
            }
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

        public void Save(RimModManagerConfig config)
        {
            Save(Path.Combine(config.GameConfigFolder!, "ModsConfig.xml"));
        }

        public void Save(string path)
        {
            using XmlWriter writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true });

            writer.WriteStartDocument();
            writer.WriteStartElement("ModsConfigData");

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

            writer.WriteEndElement(); // </ModsConfigData>
            writer.WriteEndDocument();
        }

        public void Sort()
        {
            List<RimMod> sorted = [];
            if (ModSorter.Sort(activeMods, sorted))
            {
                activeMods.Clear();
                activeMods.AddRange(sorted);
                activeModIds.Clear();
                activeModIds.AddRange(sorted.Select(x => x.PackageId));
                CheckForProblems();
            }
        }
    }
}