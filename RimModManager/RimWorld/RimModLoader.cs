namespace RimModManager.RimWorld
{
    using Hexa.NET.Logging;

    public class RimModLoader
    {
        public static RimModList LoadMods(RimModManagerConfig config)
        {
            List<RimMod> mods = [];

            if (!string.IsNullOrEmpty(config.GameFolder))
            {
                string dataFolder = Path.Combine(config.GameFolder, "Data");
                if (Directory.Exists(dataFolder))
                {
                    mods.AddRange(LoadModsFromFolder(dataFolder, ModKind.Base));
                }

                string localModFolder = Path.Combine(config.GameFolder, "Mods");
                if (Directory.Exists(localModFolder))
                {
                    mods.AddRange(LoadModsFromFolder(localModFolder, ModKind.Local));
                }

                if (!string.IsNullOrEmpty(config.SteamModFolder))
                {
                    mods.AddRange(LoadModsFromFolder(config.SteamModFolder, ModKind.Steam));
                }
            }

            return new(mods);
        }

        private static List<RimMod> LoadModsFromFolder(string folderPath, ModKind modKind)
        {
            List<RimMod> mods = [];

            if (Directory.Exists(folderPath))
            {
                foreach (var modFolder in Directory.GetDirectories(folderPath))
                {
                    var modMetadata = LoadModMetadata(modFolder);
                    if (modMetadata != null)
                    {
                        var mod = new RimMod
                        {
                            Kind = modKind,
                            Path = modFolder,
                            Metadata = modMetadata
                        };
                        if (modKind == ModKind.Steam && long.TryParse(Path.GetFileName(modFolder.AsSpan()), out var steamId))
                        {
                            mod.SteamId = steamId;
                        }
                        mods.Add(mod);
                    }
                }
            }

            return mods;
        }

        private static ModMetadata? LoadModMetadata(string modFolderPath)
        {
            string metadataPath = Path.Combine(modFolderPath, "About", "About.xml");

            if (File.Exists(metadataPath))
            {
                try
                {
                    return ModMetadata.Parse(File.ReadAllText(metadataPath));
                }
                catch (Exception ex)
                {
                    LoggerFactory.General.Fail($"Failed to parse mod '{modFolderPath}'");
                    LoggerFactory.General.Log(ex);
                }
            }

            return null;
        }
    }
}