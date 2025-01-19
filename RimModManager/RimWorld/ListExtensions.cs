namespace RimModManager.RimWorld
{
    using System.Collections.Generic;

    public static class ListExtensions
    {
        public static RimMod? FindMod(this List<RimMod> mods, string id)
        {
            foreach (var mod in mods)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(mod.PackageId, id)) return mod;
            }
            return null;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T b = list[i];
                if (item == null) continue;
                if (item.Equals(b))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}