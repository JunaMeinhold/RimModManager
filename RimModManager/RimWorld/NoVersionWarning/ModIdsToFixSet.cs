namespace RimModManager.RimWorld.NoVersionWarning
{
    using System.Collections.Generic;
    using System.Xml;

    public class NoVerWarnSet
    {
        public HashSet<string> ModIds { get; } = [];

        public static NoVerWarnSet Load(string path)
        {
            using FileStream fs = File.OpenRead(path);
            return Load(fs);
        }

        public static NoVerWarnSet Load(Stream stream)
        {
            using XmlReader xmlReader = XmlReader.Create(stream);
            NoVerWarnSet set = new();
            set.Read(xmlReader);
            return set;
        }

        public void Read(XmlReader reader)
        {
            /*
            <?xml version="1.0" encoding="utf-8"?>
            <ModIdsToFix>
                <!-- Another Milk Retexture -->
                <li>Mallow.MilkRetexture</li>
                ....
            </ModIdsToFix>
             */

            reader.MoveToContent();
            if (reader.NodeType != XmlNodeType.Element || reader.Name != "ModIdsToFix")
            {
                if (!reader.ReadToFollowing("ModIdsToFix"))
                {
                    return; // Element not found
                }
            }

            if (reader.IsEmptyElement) return;


            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "ModIdsToFix")
                {
                    break;
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "li")
                {
                    string? modId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(modId))
                    {
                        ModIds.Add(modId.Trim());
                    }
                }
            }
        }
    }
}
