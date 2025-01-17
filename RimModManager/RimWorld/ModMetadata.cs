namespace RimModManager.RimWorld
{
    using RimModManager;
    using System.Runtime.Intrinsics.Arm;
    using System.Xml.Linq;

    public class ModMetadata
    {
        public static readonly Dictionary<string, string> WellKnownNames = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ludeon.rimworld", "Core" },
            { "ludeon.rimworld.royalty", "Royalty" },
            { "ludeon.rimworld.ideology", "Ideology" },
            { "ludeon.rimworld.biotech", "Biotech" },
            { "ludeon.rimworld.anomaly", "Anomaly" },
        };

        public string PackageId { get; set; }

        public string Name { get; set; }

        public List<string> Authors { get; set; }

        public string Description { get; set; }

        public List<RimVersion> SupportedVersions { get; set; }

        public string ModVersion { get; set; }

        public string ModIconPath { get; set; }

        public string Url { get; set; }

        public Dictionary<string, string> DescriptionsByVersion { get; set; }

        public List<ModDependency> ModDependencies { get; set; }

        public Dictionary<RimVersion, List<ModDependency>> ModDependenciesByVersion { get; set; }

        public List<string> LoadBefore { get; set; }

        public Dictionary<RimVersion, List<string>> LoadBeforeByVersion { get; set; }

        public List<string> ForceLoadBefore { get; set; }

        public List<string> LoadAfter { get; set; }

        public Dictionary<RimVersion, List<string>> LoadAfterByVersion { get; set; }

        public List<string> ForceLoadAfter { get; set; }

        public List<string> IncompatibleWith { get; set; }

        public Dictionary<RimVersion, List<string>> IncompatibleWithByVersion { get; set; }

        public ModMetadata()
        {
            Authors = [];
            SupportedVersions = [];
            DescriptionsByVersion = [];
            ModDependencies = [];
            ModDependenciesByVersion = [];
            LoadBefore = [];
            LoadBeforeByVersion = [];
            ForceLoadBefore = [];
            LoadAfter = [];
            LoadAfterByVersion = [];
            ForceLoadAfter = [];
            IncompatibleWith = [];
            IncompatibleWithByVersion = [];
            PackageId = null!;
            Name = null!;
            Description = null!;
            ModVersion = null!;
            ModIconPath = null!;
            Url = null!;
        }

        public IEnumerable<ModDependency> EnumerateDependencies(RimVersion version)
        {
            foreach (var item in ModDependencies)
            {
                yield return item;
            }

            if (ModDependenciesByVersion.TryGetValue(version, out var modDependencies))
            {
                foreach (var item in modDependencies)
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<string> EnumerateIncompatibleWith(RimVersion version)
        {
            foreach (var item in IncompatibleWith)
            {
                yield return item;
            }

            if (IncompatibleWithByVersion.TryGetValue(version, out var modDependencies))
            {
                foreach (var item in modDependencies)
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<ModReference> EnumerateDependenciesAsRef(RimVersion version, IReadOnlyDictionary<string, RimMod> packageIdToMod)
        {
            foreach (var item in ModDependencies)
            {
                yield return ModReference.BuildRef(item.PackageId, packageIdToMod, ModReferenceDirection.LoadAfter, true);
            }

            if (ModDependenciesByVersion.TryGetValue(version, out var modDependencies))
            {
                foreach (var item in modDependencies)
                {
                    yield return ModReference.BuildRef(item.PackageId, packageIdToMod, ModReferenceDirection.LoadAfter, true);
                }
            }
        }

        public IEnumerable<ModReference> EnumerateLoadBefore(RimVersion version, IReadOnlyDictionary<string, RimMod> packageIdToMod)
        {
            foreach (var item in LoadBefore)
            {
                yield return ModReference.BuildRef(item, packageIdToMod, ModReferenceDirection.LoadBefore, false);
            }

            if (LoadBeforeByVersion.TryGetValue(version, out var loadBefore))
            {
                foreach (var item in loadBefore)
                {
                    yield return ModReference.BuildRef(item, packageIdToMod, ModReferenceDirection.LoadBefore, false);
                }
            }

            foreach (var item in ForceLoadBefore)
            {
                yield return ModReference.BuildRef(item, packageIdToMod, ModReferenceDirection.LoadBefore, true);
            }
        }

        public IEnumerable<ModReference> EnumerateLoadAfter(RimVersion version, IReadOnlyDictionary<string, RimMod> packageIdToMod)
        {
            foreach (var item in LoadAfter)
            {
                yield return ModReference.BuildRef(item, packageIdToMod, ModReferenceDirection.LoadAfter, false);
            }

            if (LoadAfterByVersion.TryGetValue(version, out var loadBefore))
            {
                foreach (var item in loadBefore)
                {
                    yield return ModReference.BuildRef(item, packageIdToMod, ModReferenceDirection.LoadAfter, false);
                }
            }

            foreach (var item in ForceLoadAfter)
            {
                yield return ModReference.BuildRef(item, packageIdToMod, ModReferenceDirection.LoadAfter, true);
            }
        }

        public static ModMetadata Parse(string aboutXmlContent)
        {
            var doc = XDocument.Parse(aboutXmlContent);
            var root = doc.Element("ModMetaData") ?? doc.Element("modMetaData")!;

            var metadata = new ModMetadata
            {
                PackageId = root.Element("packageId")?.Value ?? throw new FormatException("Mod metadata must have a packageId"),
                Name = root.Element("name")?.Value!,
                Description = root.Element("description")?.Value!,
                ModVersion = root.Element("modVersion")?.Value!,
                ModIconPath = root.Element("modIconPath")?.Value!,
                Url = root.Element("url")?.Value!
            };

            if (WellKnownNames.TryGetValue(metadata.PackageId, out var name))
            {
                metadata.Name = name;
            }

            // Parsing authors
            var authorsElement = root.Element("authors");
            if (authorsElement != null)
            {
                foreach (var author in authorsElement.Elements("li"))
                {
                    metadata.Authors.Add(author.Value);
                }
            }
            else
            {
                var author = root.Element("author")?.Value;
                if (!string.IsNullOrEmpty(author))
                {
                    metadata.Authors.Add(author);
                }
            }

            var supportedVersionsElement = root.Element("supportedVersions");
            if (supportedVersionsElement != null)
            {
                foreach (var version in supportedVersionsElement.Elements("li"))
                {
                    metadata.SupportedVersions.Add(RimVersion.Parse(version.Value));
                }
            }

            var descriptionsByVersionElement = root.Element("descriptionsByVersion");
            if (descriptionsByVersionElement != null)
            {
                foreach (var versionElement in descriptionsByVersionElement.Elements())
                {
                    metadata.DescriptionsByVersion[versionElement.Name.LocalName] = versionElement.Value;
                }
            }

            var modDependenciesElement = root.Element("modDependencies");
            if (modDependenciesElement != null)
            {
                foreach (var dep in modDependenciesElement.Elements("li"))
                {
                    var dependency = new ModDependency
                    {
                        PackageId = dep.Element("packageId")?.Value!,
                        DisplayName = dep.Element("displayName")?.Value!,
                        SteamWorkshopUrl = dep.Element("steamWorkshopUrl")?.Value!,
                        DownloadUrl = dep.Element("downloadUrl")?.Value!
                    };
                    metadata.ModDependencies.Add(dependency);
                }
            }

            var modDependenciesByVersionElement = root.Element("modDependenciesByVersion");
            if (modDependenciesByVersionElement != null)
            {
                foreach (var versionElement in modDependenciesByVersionElement.Elements())
                {
                    var version = versionElement.Name.LocalName;
                    var dependenciesList = new List<ModDependency>();
                    foreach (var dep in versionElement.Elements("li"))
                    {
                        var dependency = new ModDependency
                        {
                            PackageId = dep.Element("packageId")?.Value!,
                            DisplayName = dep.Element("displayName")?.Value!,
                            SteamWorkshopUrl = dep.Element("steamWorkshopUrl")?.Value!,
                            DownloadUrl = dep.Element("downloadUrl")?.Value!
                        };
                        dependenciesList.Add(dependency);
                    }
                    metadata.ModDependenciesByVersion[RimVersion.Parse(version.AsSpan()[1..])] = dependenciesList;
                }
            }

            metadata.LoadBefore = ParseModList(root, "loadBefore");
            metadata.ForceLoadBefore = ParseModList(root, "forceLoadBefore");
            metadata.LoadAfter = ParseModList(root, "loadAfter");
            metadata.ForceLoadAfter = ParseModList(root, "forceLoadAfter");
            metadata.IncompatibleWith = ParseModList(root, "incompatibleWith");

            metadata.LoadBeforeByVersion = ParseModListByVersion(root, "loadBeforeByVersion");
            metadata.LoadAfterByVersion = ParseModListByVersion(root, "loadAfterByVersion");
            metadata.IncompatibleWithByVersion = ParseModListByVersion(root, "incompatibleWithByVersion");

            return metadata;
        }

        private static List<string> ParseModList(XElement root, string tagName)
        {
            var list = new List<string>();
            var element = root.Element(tagName);
            if (element != null)
            {
                foreach (var item in element.Elements("li"))
                {
                    list.Add(item.Value);
                }
            }
            return list;
        }

        private static Dictionary<RimVersion, List<string>> ParseModListByVersion(XElement root, string tagName)
        {
            var dict = new Dictionary<RimVersion, List<string>>();
            var element = root.Element(tagName);
            if (element != null)
            {
                foreach (var versionElement in element.Elements())
                {
                    var version = versionElement.Name.LocalName;
                    var modsList = new List<string>();
                    foreach (var mod in versionElement.Elements("li"))
                    {
                        modsList.Add(mod.Value);
                    }
                    dict[RimVersion.Parse(version.AsSpan()[1..])] = modsList;
                }
            }
            return dict;
        }
    }
}