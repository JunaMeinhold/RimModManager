namespace RimModManager.RimWorld.Sorting
{
    using RimModManager.RimWorld;
    using System.Collections.Generic;

    public class AlphabeticalSorter
    {
        public static void Sort(List<RimMod> activeMods, List<RimMod> modsLoadOrder)
        {
            foreach (var mod in activeMods.OrderByDescending(mod => mod.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!modsLoadOrder.Contains(mod))
                {
                    modsLoadOrder.Add(mod);
                    int indexJustAppended = modsLoadOrder.Count - 1;
                    RecursivelyForceInsert(modsLoadOrder, mod, activeMods, indexJustAppended);
                }
            }
        }

        public static void RecursivelyForceInsert(List<RimMod> modsLoadOrder, RimMod mod, List<RimMod> activeMods, int indexJustAppended)
        {
            var depsOfPackage = mod.Dependencies;

            foreach (var depMod in depsOfPackage.OrderByDescending(mod => mod.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!modsLoadOrder.Contains(depMod))
                {
                    int indexToInsertAt = indexJustAppended;
                    for (int i = indexJustAppended - 1; i >= 0; i--)
                    {
                        if (depMod.Dependencies.Contains(modsLoadOrder[i]))
                        {
                            indexToInsertAt = i + 1;
                            break;
                        }
                    }

                    modsLoadOrder.Insert(indexToInsertAt, depMod);
                    RecursivelyForceInsert(modsLoadOrder, depMod, activeMods, indexToInsertAt);
                }
            }
        }
    }
}