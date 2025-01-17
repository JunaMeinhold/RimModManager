namespace RimModManager.RimWorld.Fluffy
{
    using System.Collections.Generic;
    using System.Xml;

    public class FluffyModManifest
    {
        public string Identifier { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public List<FluffyModDependency> Dependencies { get; set; } = [];

        public List<FluffyModDependency> IncompatibleWith { get; set; } = [];

        public List<FluffyModDependency> LoadBefore { get; set; } = [];

        public List<FluffyModDependency> LoadAfter { get; set; } = [];

        public List<string> Suggests { get; set; } = [];

        public bool ShowCrossPromotions { get; set; }

        public string ManifestUri { get; set; } = string.Empty;

        public string DownloadUri { get; set; } = string.Empty;

        public static FluffyModManifest Parse(string path)
        {
            FluffyModManifest manifest = new();
            using var fs = File.OpenRead(path);
            using var reader = XmlReader.Create(fs);

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "identifier":
                            manifest.Identifier = reader.ReadElementContentAsString();
                            break;

                        case "version":
                            manifest.Version = reader.ReadElementContentAsString();
                            break;

                        case "showCrossPromotions":
                            manifest.ShowCrossPromotions = reader.ReadElementContentAsBoolean();
                            break;

                        case "manifestUri":
                            manifest.ManifestUri = reader.ReadElementContentAsString();
                            break;

                        case "downloadUri":
                            manifest.DownloadUri = reader.ReadElementContentAsString();
                            break;

                        case "dependencies":
                            ReadDependencies(reader, "dependencies", manifest.Dependencies);
                            break;

                        case "incompatibleWith":
                            ReadDependencies(reader, "incompatibleWith", manifest.IncompatibleWith);
                            break;

                        case "loadBefore":
                            ReadDependencies(reader, "loadBefore", manifest.LoadBefore);
                            break;

                        case "loadAfter":
                            ReadDependencies(reader, "loadAfter", manifest.LoadAfter);
                            break;

                        case "suggests":
                            ReadList(reader, "suggests", manifest.Suggests);
                            break;
                    }
                }
            }

            return manifest;
        }

        private static void ReadDependencies(XmlReader reader, string parentTag, List<FluffyModDependency> list)
        {
            if (reader.IsEmptyElement) return;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == parentTag)
                {
                    break;
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "li")
                {
                    FluffyModDependency dependency = new();
                    dependency.Read(reader);
                    list.Add(dependency);
                }
            }
        }

        private static void ReadList(XmlReader reader, string parentTag, List<string> list)
        {
            if (reader.IsEmptyElement) return;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == parentTag)
                {
                    break;
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "li")
                {
                    list.Add(reader.ReadElementContentAsString());
                }
            }
        }
    }
}