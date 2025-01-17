namespace RimModManager.RimWorld.Sorting
{
    using RimModManager.RimWorld;
    using System.Collections.Generic;

    public class ModDependencys
    {
        private static readonly HashSet<string> knownTierOneMods = new(StringComparer.OrdinalIgnoreCase)
        {
                "zetrith.prepatcher",
                "brrainz.harmony",
                "me.samboycoding.betterloading.dev",
                "ludeon.rimworld",
                "ludeon.rimworld.royalty",
                "ludeon.rimworld.ideology",
                "ludeon.rimworld.biotech",
                "ludeon.rimworld.anomaly",
                "unlimitedhugs.hugslib"
        };

        public static (Dictionary<RimMod, HashSet<RimMod>>, HashSet<RimMod>) GenTierOneDepsGraph(List<RimMod> allMods)
        {
            HashSet<RimMod> processedMods = [];
            HashSet<RimMod> tierOneMods = [];

            foreach (var mod in allMods.Where(m => knownTierOneMods.Contains(m.PackageId)))
            {
                if (tierOneMods.Add(mod))
                {
                    AddDependenciesRecursive(mod, tierOneMods);
                }
            }

            Dictionary<RimMod, HashSet<RimMod>> tierOneDependencyGraph = [];

            foreach (var mod in tierOneMods)
            {
                HashSet<RimMod> deps = [];
                foreach (var d in mod.Dependencies)
                {
                    if (tierOneMods.Contains(d))
                    {
                        deps.Add(d);
                    }
                }
                tierOneDependencyGraph.Add(mod, deps);
            }

            return (tierOneDependencyGraph, tierOneMods);
        }

        private static void AddDependenciesRecursive(RimMod mod, HashSet<RimMod> tierOneMods)
        {
            foreach (RimMod dependency in mod.Dependencies.Where(d => !tierOneMods.Contains(d)))
            {
                tierOneMods.Add(dependency);
                AddDependenciesRecursive(dependency, tierOneMods);
            }
        }

        private static readonly HashSet<string> KnownTierThreeMods = new(StringComparer.OrdinalIgnoreCase)
        {
            "krkr.rocketman"
        };

        private static IEnumerable<string> GetKnownTierThreeMods(IEnumerable<RimMod> allMods)
        {
            foreach (var mod in allMods.Where(mod => mod.LoadBottom == true))
            {
                yield return mod.PackageId;
            }

            foreach (var mod in KnownTierThreeMods)
            {
                yield return mod;
            }
        }

        public static (Dictionary<RimMod, HashSet<RimMod>>, HashSet<RimMod>) GenTierThreeDepsGraph(List<RimMod> allMods)
        {
            HashSet<RimMod> tierThreeMods = [];

            foreach (var knownTierThreeMod in GetKnownTierThreeMods(allMods))
            {
                var mod = allMods.FindMod(knownTierThreeMod);
                if (mod != null)
                {
                    tierThreeMods.Add(mod);
                    foreach (var dependency in GetReverseDependenciesRecursive(mod))
                    {
                        tierThreeMods.Add(dependency);
                    }
                }
            }

            Dictionary<RimMod, HashSet<RimMod>> tierThreeDependencyGraph = [];

            foreach (var tierThreeMod in tierThreeMods)
            {
                tierThreeDependencyGraph[tierThreeMod] = [];

                foreach (var possibleAdd in tierThreeMod.Dependencies)
                {
                    if (tierThreeMods.Contains(possibleAdd))
                    {
                        tierThreeDependencyGraph[tierThreeMod].Add(possibleAdd);
                    }
                }
            }

            return (tierThreeDependencyGraph, tierThreeMods);
        }

        private static IEnumerable<RimMod> GetReverseDependenciesRecursive(RimMod mod)
        {
            foreach (var dependent in mod.Dependants)
            {
                yield return dependent;
                foreach (var item in GetReverseDependenciesRecursive(dependent))
                {
                    yield return item;
                }
            }
        }

        public static Dictionary<RimMod, HashSet<RimMod>> GenTierTwoDepsGraph(List<RimMod> activeMods, HashSet<RimMod> tierOneMods, HashSet<RimMod> tierThreeMods)
        {
            Dictionary<RimMod, HashSet<RimMod>> tierTwoDependencyGraph = [];

            foreach (var mod in activeMods)
            {
                if (!tierOneMods.Contains(mod) && !tierThreeMods.Contains(mod))
                {
                    HashSet<RimMod> strippedDependencies = [];

                    foreach (var dependency in mod.Dependencies)
                    {
                        if (!tierOneMods.Contains(dependency) && !tierThreeMods.Contains(dependency) && activeMods.Contains(dependency))
                        {
                            strippedDependencies.Add(dependency);
                        }
                    }

                    tierTwoDependencyGraph[mod] = strippedDependencies;
                }
            }

            return tierTwoDependencyGraph;
        }
    }
}