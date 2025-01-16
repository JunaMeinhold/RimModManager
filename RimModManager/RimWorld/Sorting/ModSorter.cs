namespace RimModManager.RimWorld.Sorting
{
    using RimModManager.RimWorld;
    using System.Collections.Generic;

    public class ModSorter
    {
        public static bool Sort(List<RimMod> activeMods, List<RimMod> sortedMods)
        {
            foreach (var mod in activeMods)
            {
                mod.ClearSortingState();
            }

            foreach (var mod in activeMods)
            {
                foreach (var reference in mod.LoadBefore)
                {
                    var dependant = reference.Mod;
                    if (!dependant.IsActive || mod.Dependants.Contains(dependant)) continue;

                    mod.Dependants.Add(dependant);
                    dependant.Dependencies.Add(mod);
                }

                foreach (var reference in mod.LoadAfter)
                {
                    var dependency = reference.Mod;
                    if (!dependency.IsActive || mod.Dependencies.Contains(dependency)) continue;

                    dependency.Dependants.Add(mod);
                    mod.Dependencies.Add(dependency);
                }
            }

            //AlphaSorter.Sort(activeMods, mods);

            var d1 = ModDependencys.GenTierOneDepsGraph(activeMods);
            var d3 = ModDependencys.GenTierThreeDepsGraph(activeMods);
            var d2 = ModDependencys.GenTierTwoDepsGraph(activeMods, d1.Item2, d3.Item2);

            TopologicalSorterDFS<RimMod> sorter = new();
            Sort(sorter, d1.Item2, d1.Item1, sortedMods);
            Sort(sorter, d2, sortedMods);
            Sort(sorter, d3.Item2, d3.Item1, sortedMods);

            return true;
        }

        private static void Sort(TopologicalSorterDFS<RimMod> sorter, HashSet<RimMod> mods, Dictionary<RimMod, HashSet<RimMod>> edges, List<RimMod> sortedMods)
        {
            foreach (var mod in mods)
            {
                mod.Dependencies.Clear();
                mod.Dependencies.AddRange(edges[mod]);
            }
            sorter.TopologicalSort(mods, sortedMods);
        }

        private static void Sort(TopologicalSorterDFS<RimMod> sorter, Dictionary<RimMod, HashSet<RimMod>> edges, List<RimMod> sortedMods)
        {
            foreach (var pair in edges)
            {
                var mod = pair.Key;
                mod.Dependencies.Clear();
                mod.Dependencies.AddRange(pair.Value);
            }
            sorter.TopologicalSort(edges.Keys, sortedMods);
        }

        private static void CollectDependants(HashSet<RimMod> dependants, RimMod current)
        {
            foreach (var dependant in current.Dependants)
            {
                if (!dependant.IsActive) continue;
                if (!dependants.Add(dependant)) continue;
                CollectDependants(dependants, dependant);
            }
        }
    }
}