namespace RimModManager.RimWorld
{
    using RimModManager;
    using RimModManager.RimWorld.Profiles;
    using RimModManager.RimWorld.Rules;
    using RimModManager.RimWorld.Sorting;
    using RimModManager.RimWorld.SteamDB;
    using System.Collections;
    using System.Collections.Generic;

    public class RimLoadOrder : IEnumerable<RimMod>
    {
        private readonly RimModList modList;
        private readonly List<string> activeModsOrder = [];
        private readonly List<RimMod> activeMods = [];
        private readonly List<RimMod> inactiveMods = [];
        private readonly List<string> knownExpansions = [];
        private readonly HashSet<string> activeModIds = new(StringComparer.OrdinalIgnoreCase);

        public RimLoadOrder(RimModList modList)
        {
            this.modList = modList;
        }

        public RimMessageCollection Messages = [];

        public IReadOnlyList<string> ActiveModsOrder => activeModsOrder;

        public IReadOnlyList<RimMod> ActiveMods => activeMods;

        public IReadOnlySet<string> ActiveModIds => activeModIds;

        public List<string> KnownExpansions => knownExpansions;

        public int ErrorsCount => Messages.ErrorsCount;

        public IReadOnlyList<RimMod> InactiveMods => inactiveMods;

        public RimVersion RimVersion => RimVersion.Parse(Version);

        public string Version { get; set; } = string.Empty;

        public int WarningsCount => Messages.WarningsCount;

        public void PopulateList(RimModList modList)
        {
            inactiveMods.Clear();

            foreach (var mod in modList)
            {
                if (!activeModIds.Contains(mod.PackageId))
                {
                    inactiveMods.Add(mod);
                    mod.IsActive = false;
                }
            }

            inactiveMods.Sort(AZNameComparer.Instance);

            var currentVersion = RimVersion.ToCompareVersion();
            ResolveModsDeps(currentVersion, modList, modList.PackageIdToMod, modList.SteamIdToMod);
            CheckForProblems();
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

        public void ActivateMod(string packageId)
        {
            if (!modList.TryGetMod(packageId, out var mod))
            {
                mod = RimMod.CreateUnknown(packageId);
            }
            if (mod.IsActive) return;
            activeModsOrder.Add(packageId);
            activeModIds.Add(packageId);
            activeMods.Add(mod);
            inactiveMods.Remove(mod);
            mod.IsActive = true;

            CheckForProblems();
        }

        public void ActivateMods(IEnumerable<string> packageIds)
        {
            foreach (string packageId in packageIds)
            {
                if (!modList.TryGetMod(packageId, out var mod))
                {
                    mod = RimMod.CreateUnknown(packageId);
                }
                if (mod.IsActive) continue;

                activeModsOrder.Add(packageId);
                activeModIds.Add(packageId);
                activeMods.Add(mod);
                inactiveMods.Remove(mod);
                mod.IsActive = true;
            }

            CheckForProblems();
        }

        public void ActivateMod(RimMod mod)
        {
            if (mod.IsActive) return;
            activeModsOrder.Add(mod.PackageId);
            activeModIds.Add(mod.PackageId);
            activeMods.Add(mod);
            inactiveMods.Remove(mod);
            mod.IsActive = true;

            CheckForProblems();
        }

        public void ActivateMods(IEnumerable<RimMod> mods)
        {
            foreach (var mod in mods)
            {
                if (mod.IsActive) continue;
                activeModsOrder.Add(mod.PackageId);
                activeModIds.Add(mod.PackageId);
                activeMods.Add(mod);
                inactiveMods.Remove(mod);
                mod.IsActive = true;
            }

            CheckForProblems();
        }

        public void Apply(RimProfile profile, RimModList mods)
        {
            Version = profile.Version;
            activeModsOrder.Clear();
            activeModIds.Clear();
            foreach (var packageId in profile.ActiveModOrder)
            {
                activeModsOrder.Add(packageId);
                activeModIds.Add(packageId);
            }

            knownExpansions.Clear();
            knownExpansions.AddRange(profile.KnownExpansions);
            PopulateList(mods);
        }

        public void CheckForProblems()
        {
            ProblemChecker.CheckForProblems(Messages, this, modList, RimVersion, true);
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
                activeModsOrder.RemoveAt(i);
                activeModIds.Remove(mod.PackageId);
                inactiveMods.Add(mod);
                mod.IsActive = false;
            }
            CheckForProblems();
        }

        public void ToggleMod(RimMod mod)
        {
            if (mod.IsActive)
            {
                DeactiveMod(mod);
            }
            else
            {
                ActivateMod(mod);
            }
        }

        public void DeactiveMod(RimMod mod)
        {
            if (!mod.IsActive) return;
            RemoveId(mod.PackageId);
            activeMods.Remove(mod);
            activeModIds.Remove(mod.PackageId);
            inactiveMods.Add(mod);
            mod.IsActive = false;

            CheckForProblems();
        }

        public void DeactiveMods(IEnumerable<RimMod> mods)
        {
            foreach (var mod in mods)
            {
                if (!mod.IsActive) continue;
                RemoveId(mod.PackageId);
                activeMods.Remove(mod);
                activeModIds.Remove(mod.PackageId);
                inactiveMods.Add(mod);
                mod.IsActive = false;
            }

            CheckForProblems();
        }

        public IEnumerable<RimMod> FindMissingDependencies(RimModList mods)
        {
            if (mods.TryGetMod(RimMod.CorePackageId, out var coreMod) && !coreMod.IsActive)
            {
                yield return coreMod;
            }

            var version = RimVersion.ToCompareVersion();
            foreach (var mod in activeMods)
            {
                foreach (var dep in mod.Metadata.EnumerateDependencies(version))
                {
                    if (!mods.TryGetMod(dep.PackageId, out var depMod))
                    {
                        depMod = RimMod.CreateUnknown(dep.PackageId, dep.DisplayName, null);
                        depMod.Metadata.Url = dep.DownloadUrl;
                        if (dep.SteamWorkshopUrl != null)
                        {
                            int idx = dep.SteamWorkshopUrl.LastIndexOf('/');
                            if (idx != -1 && idx + 1 < dep.SteamWorkshopUrl.Length)
                            {
                                var span = dep.SteamWorkshopUrl.AsSpan(idx + 1);
                                if (long.TryParse(span, out var steamId))
                                {
                                    depMod.SteamId = steamId;
                                }
                            }
                        }
                    }

                    if (!depMod.IsActive)
                    {
                        yield return depMod;
                    }
                }
            }
        }

        public void Move(RimMod mod, int index)
        {
            if (mod.IsActive)
            {
                int oldIndex = activeMods.IndexOf(mod);
                if (oldIndex == -1) throw new ArgumentException("Item was not found in list.", nameof(mod));
                activeMods.RemoveAt(oldIndex);
                activeModsOrder.RemoveAt(oldIndex);

                activeMods.Insert(index, mod);
                activeModsOrder.Insert(index, mod.PackageId);
                CheckForProblems();
            }
        }

        public void Sort()
        {
            List<RimMod> sorted = [];
            if (ModSorter.Sort(activeMods, sorted))
            {
                activeMods.Clear();
                activeMods.AddRange(sorted);
                activeModsOrder.Clear();
                activeModsOrder.AddRange(sorted.Select(x => x.PackageId));
                CheckForProblems();
            }
        }

        private void RemoveId(string id)
        {
            for (int i = 0; i < activeModsOrder.Count; i++)
            {
                string dd = activeModsOrder[i];
                if (StringComparer.OrdinalIgnoreCase.Compare(dd, id) == 0)
                {
                    activeModsOrder.RemoveAt(i);
                    activeModIds.Remove(id);
                    return;
                }
            }
        }

        public bool Contains(string packageId)
        {
            return activeModIds.Contains(packageId);
        }

        public int IndexOf(RimMod mod)
        {
            return activeMods.IndexOf(mod);
        }

        public IEnumerator<RimMod> GetEnumerator()
        {
            return activeMods.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return activeMods.GetEnumerator();
        }
    }
}