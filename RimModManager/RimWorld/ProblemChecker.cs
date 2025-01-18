namespace RimModManager.RimWorld
{
    using RimModManager;
    using System.Collections.Generic;

    public class ProblemChecker
    {
        public static void CheckForProblems(RimMessageCollection messages, List<RimMod> activeMods, List<RimMod> inactiveMods, List<string> activeModIds, RimVersion version, bool addMessagesToMods = false)
        {
            Dictionary<string, RimMod> packageIdToMod = new(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in activeMods)
            {
                packageIdToMod[mod.PackageId] = mod;
            }
            foreach (var mod in inactiveMods)
            {
                packageIdToMod[mod.PackageId] = mod;
            }

            HashSet<string> activeModsIdsSet = new(StringComparer.OrdinalIgnoreCase);
            foreach (var id in activeModIds)
            {
                activeModsIdsSet.Add(id);
            }

            CheckForProblems(messages, activeMods, packageIdToMod, activeModsIdsSet, version, addMessagesToMods);
        }

        public static void CheckForProblems(RimMessageCollection messages, List<RimMod> activeMods, IReadOnlyDictionary<string, RimMod> packageIdToMod, List<string> activeModIds, RimVersion version, bool addMessagesToMods = false)
        {
            HashSet<string> activeModsIdsSet = new(StringComparer.OrdinalIgnoreCase);
            foreach (var id in activeModIds)
            {
                activeModsIdsSet.Add(id);
            }

            CheckForProblems(messages, activeMods, packageIdToMod, activeModsIdsSet, version, addMessagesToMods);
        }

        public static void CheckForProblems(RimMessageCollection messages, List<RimMod> activeMods, IReadOnlyDictionary<string, RimMod> packageIdToMod, HashSet<string> activeModIds, RimVersion version, bool addMessagesToMods = false)
        {
            messages.AddMessagesToMods = addMessagesToMods;
            messages.Clear();

            if (packageIdToMod.TryGetValue(RimMod.CorePackageId, out var coreMod) && !coreMod.IsActive)
            {
                messages.AddMessage(coreMod, "Core mod not loaded.", RimSeverity.Error);
            }

            var currentVersion = version.ToCompareVersion();
            foreach (var mod in activeMods)
            {
                if (addMessagesToMods)
                {
                    mod.ClearMessages();
                }

                CheckGameVersion(messages, currentVersion, mod);
                CheckDependencies(messages, currentVersion, activeModIds, mod);
                CheckIncompatibleMods(messages, currentVersion, activeModIds, packageIdToMod, mod);
                CheckLoadOrder(messages, activeMods, mod);
            }
        }

        private static void CheckGameVersion(RimMessageCollection messages, RimVersion currentVersion, RimMod mod)
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
                messages.AddMessage(mod, "Incompatible game version.", RimSeverity.Warn);
            }
        }

        private static void CheckDependencies(RimMessageCollection messages, RimVersion version, HashSet<string> activeModsIds, RimMod mod)
        {
            foreach (var deps in mod.Metadata.EnumerateDependencies(version))
            {
                if (!activeModsIds.Contains(deps.PackageId))
                {
                    messages.AddMessage(mod, $"Mod depends on {deps.DisplayName} ({deps.PackageId}) but it's not loaded", RimSeverity.Error);
                }
            }
        }

        private static void CheckIncompatibleMods(RimMessageCollection messages, RimVersion version, HashSet<string> activeModsIds, IReadOnlyDictionary<string, RimMod> packageIdToMod, RimMod mod)
        {
            foreach (var incompatibleId in mod.Metadata.EnumerateIncompatibleWith(version))
            {
                if (activeModsIds.Contains(incompatibleId))
                {
                    messages.AddMessage(mod, $"Mod is incompatible with {packageIdToMod[incompatibleId].Name}", RimSeverity.Error);
                }
            }
        }

        private static void CheckLoadOrder(RimMessageCollection messages, List<RimMod> activeMods, RimMod mod)
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
                        messages.AddMessage(mod, $"Mod must load before {reference.Mod.Name} ({reference.Mod.PackageId})", RimSeverity.Error);
                    }
                    else
                    {
                        messages.AddMessage(mod, $"Mod should load before {reference.Mod.Name} ({reference.Mod.PackageId})", RimSeverity.Warn);
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
                        messages.AddMessage(mod, $"Mod must load after {reference.Mod.Name} ({reference.Mod.PackageId})", RimSeverity.Error);
                    }
                    else
                    {
                        messages.AddMessage(mod, $"Mod should load after {reference.Mod.Name} ({reference.Mod.PackageId})", RimSeverity.Warn);
                    }
                }
            }
        }
    }
}