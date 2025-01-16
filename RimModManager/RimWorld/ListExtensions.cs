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
    }
}