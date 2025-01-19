namespace RimModManager.RimWorld
{
    using RimModManager;
    using RimModManager.RimWorld.Profiles;
    using System.Collections.Generic;

    public class ProblemChecker
    {
        public static void CheckForProblems(RimMessageCollection messages, RimLoadOrder loadOrder, RimModList mods, RimVersion version, bool addMessagesToMods = false)
        {
            messages.AddMessagesToMods = addMessagesToMods;
            messages.Clear();

            var packageIdToMod = mods.PackageIdToMod;

            if (packageIdToMod.TryGetValue(RimMod.CorePackageId, out var coreMod) && !coreMod.IsActive)
            {
                messages.AddMessage(coreMod, "Core mod not loaded.", RimSeverity.Error);
            }

            var currentVersion = version.ToCompareVersion();

            foreach (var mod in mods)
            {
                if (addMessagesToMods)
                {
                    mod.ClearMessages();
                }

                CheckGameVersion(messages, currentVersion, mod);
            }

            foreach (var mod in loadOrder)
            {
                CheckDependencies(messages, currentVersion, loadOrder.ActiveModIds, mod);
                CheckIncompatibleMods(messages, currentVersion, loadOrder.ActiveModIds, packageIdToMod, mod);
                CheckLoadOrder(messages, loadOrder.ActiveMods, mod);
            }
        }

        public static void CheckForProblems(RimMessageCollection messages, RimProfile profile, RimModList mods, RimVersion version, bool addMessagesToMods = false)
        {
            messages.AddMessagesToMods = addMessagesToMods;
            messages.Clear();

            var packageIdToMod = mods.PackageIdToMod;

            if (packageIdToMod.TryGetValue(RimMod.CorePackageId, out var coreMod) && !coreMod.IsActive)
            {
                messages.AddMessage(coreMod, "Core mod not loaded.", RimSeverity.Error);
            }

            var currentVersion = version.ToCompareVersion();

            foreach (var mod in mods)
            {
                if (addMessagesToMods)
                {
                    mod.ClearMessages();
                }

                CheckGameVersion(messages, currentVersion, mod);
            }

            foreach (var mod in profile.ActiveMods)
            {
                CheckDependencies(messages, currentVersion, profile.ActiveModIds, mod);
                CheckIncompatibleMods(messages, currentVersion, profile.ActiveModIds, packageIdToMod, mod);
                CheckLoadOrder(messages, profile.ActiveMods, mod);
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

        private static void CheckDependencies(RimMessageCollection messages, RimVersion version, IReadOnlySet<string> loadOrder, RimMod mod)
        {
            foreach (var deps in mod.Metadata.EnumerateDependencies(version))
            {
                if (!loadOrder.Contains(deps.PackageId))
                {
                    messages.AddMessage(mod, $"Mod depends on {deps.DisplayName} ({deps.PackageId}) but it's not loaded", RimSeverity.Error);
                }
            }
        }

        private static void CheckIncompatibleMods(RimMessageCollection messages, RimVersion version, IReadOnlySet<string> loadOrder, IReadOnlyDictionary<string, RimMod> packageIdToMod, RimMod mod)
        {
            foreach (var incompatibleId in mod.Metadata.EnumerateIncompatibleWith(version))
            {
                if (loadOrder.Contains(incompatibleId))
                {
                    messages.AddMessage(mod, $"Mod is incompatible with {packageIdToMod[incompatibleId].Name}", RimSeverity.Error);
                }
            }
        }

        private static void CheckLoadOrder(RimMessageCollection messages, IReadOnlyList<RimMod> loadOrder, RimMod mod)
        {
            int index = loadOrder.IndexOf(mod);
            foreach (var reference in mod.LoadBefore)
            {
                int otherIndex = loadOrder.IndexOf(reference.Mod);
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
                int otherIndex = loadOrder.IndexOf(reference.Mod);
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