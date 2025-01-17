namespace RimModManager.RimWorld
{
    using System.Xml;

    public readonly struct RimSaveGameComparer : IComparer<RimSaveGame>
    {
        public static readonly RimSaveGameComparer Instance = new();

        public int Compare(RimSaveGame? x, RimSaveGame? y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentException("Cannot compare null objects.");
            }

            return y.CreateDate.CompareTo(x.CreateDate);
        }
    }

    public class RimSaveGame
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public DateTime CreateDate { get; set; }

        public RimSaveGameMetadata Metadata { get; set; } = new();

        public List<RimMod> Mods { get; set; } = [];

        public static RimSaveGame Load(string path, RimModList mods)
        {
            using var fs = File.OpenRead(path);
            using XmlReader reader = XmlReader.Create(fs);
            RimSaveGame saveGame = new()
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(path),
                Path = path,
                CreateDate = File.GetLastWriteTime(path)
            };

            while (reader.Read())
            {
                if (reader.IsStartElement("meta"))
                {
                    saveGame.Metadata.Read(reader);
                    break;
                }
            }

            for (int i = 0; i < saveGame.Metadata.ModIds.Count; i++)
            {
                string packageId = saveGame.Metadata.ModIds[i];
                if (!mods.TryGetMod(packageId, out var mod))
                {
                    var name = saveGame.Metadata.ModNames[i];
                    long? steamId = saveGame.Metadata.ModSteamIds[i];
                    if (steamId == 0)
                    {
                        steamId = null;
                    }

                    mod = RimMod.CreateUnknown(packageId, name, steamId);
                }

                saveGame.Mods.Add(mod);
            }

            return saveGame;
        }
    }
}