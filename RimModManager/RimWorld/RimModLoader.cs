namespace RimModManager.RimWorld
{
    using Hexa.NET.Logging;
    using RimModManager.RimWorld.Fluffy;

    public class RimModLoader
    {
        public static RimModList Current { get; private set; } = null!;

        private static RimModList LoadMods(RimModManagerConfig config)
        {
            if (string.IsNullOrEmpty(config.GameFolder))
            {
                return new([]);
            }

            List<RimMod> mods = [];

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

            return new(mods);
        }

        private static IEnumerable<RimMod> LoadModsFromFolder(string folderPath, ModKind modKind)
        {
            if (Directory.Exists(folderPath))
            {
                foreach (var modFolder in Directory.GetDirectories(folderPath))
                {
                    var mod = LoadMod(modKind, modFolder);
                    if (mod != null)
                    {
                        yield return mod;
                    }
                }
            }
        }

        private static RimMod? LoadMod(ModKind modKind, string modFolder)
        {
            var modMetadata = LoadModMetadata(modFolder);
            var modManifest = LoadFluffyModManifest(modFolder);
            if (modMetadata != null)
            {
                var mod = new RimMod
                {
                    Kind = modKind,
                    Path = modFolder,
                    Metadata = modMetadata,
                    FluffyManifest = modManifest,
                };

                if (modKind == ModKind.Steam && long.TryParse(Path.GetFileName(modFolder.AsSpan()), out var steamId))
                {
                    mod.SteamId = steamId;
                }

                return mod;
            }
            return null;
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
                    LoggerFactory.General.Fail($"Failed to parse mod About.xml '{modFolderPath}'");
                    LoggerFactory.General.Log(ex);
                }
            }

            return null;
        }

        private static FluffyModManifest? LoadFluffyModManifest(string modFolderPath)
        {
            string fluffyManifest = Path.Combine(modFolderPath, "About", "Manifest.xml");

            if (File.Exists(fluffyManifest))
            {
                try
                {
                    return FluffyModManifest.Parse(fluffyManifest);
                }
                catch (Exception ex)
                {
                    LoggerFactory.General.Fail($"Failed to parse mod Manifest.xml '{modFolderPath}'");
                    LoggerFactory.General.Log(ex);
                }
            }

            return null;
        }

        public static void RefreshMods(RimModManagerConfig config)
        {
            if (string.IsNullOrEmpty(config.GameFolder))
            {
                return;
            }

            if (Current == null)
            {
                Current = LoadMods(config);
                return;
            }

            lock (Current.SyncRoot)
            {
                var existingMods = Current.ToDictionary(mod => mod.Path!, mod => mod);

                string dataFolder = Path.Combine(config.GameFolder, "Data");
                RefreshModsInFolder(dataFolder, ModKind.Base, existingMods, Current);

                string localModFolder = Path.Combine(config.GameFolder, "Mods");
                RefreshModsInFolder(localModFolder, ModKind.Local, existingMods, Current);

                if (!string.IsNullOrEmpty(config.SteamModFolder))
                {
                    RefreshModsInFolder(config.SteamModFolder, ModKind.Steam, existingMods, Current);
                }

                // Remove mods that no longer exist
                var removedPaths = existingMods.Keys.Except(Current.Select(mod => mod.Path!)).ToList();
                foreach (var path in removedPaths)
                {
                    Current.Remove(existingMods[path]);
                }
            }
        }

        private static void RefreshModsInFolder(string folderPath, ModKind modKind, Dictionary<string, RimMod> existingMods, RimModList mods)
        {
            if (Directory.Exists(folderPath))
            {
                foreach (var modFolder in Directory.GetDirectories(folderPath))
                {
                    if (existingMods.TryGetValue(modFolder, out var existingMod))
                    {
                        if (!UpdateModMetadata(existingMod, modFolder))
                        {
                            mods.Remove(existingMod);
                        }
                    }
                    else
                    {
                        var mod = LoadMod(modKind, modFolder);
                        if (mod != null)
                        {
                            Current.Add(mod);
                        }
                    }
                }
            }
        }

        private static bool UpdateModMetadata(RimMod existingMod, string modFolder)
        {
            var modMetadata = LoadModMetadata(modFolder);
            if (modMetadata == null)
            {
                return false;
            }

            if (!existingMod.Metadata.Equals(modMetadata))
            {
                existingMod.Metadata = modMetadata;
            }

            var modManifest = LoadFluffyModManifest(modFolder);

            if (modManifest != null && (existingMod.FluffyManifest == null || !existingMod.FluffyManifest.Equals(modManifest)))
            {
                existingMod.FluffyManifest = modManifest;
            }

            return true;
        }
    }
}