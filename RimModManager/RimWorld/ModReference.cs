namespace RimModManager.RimWorld
{
    public enum ModReferenceDirection
    {
        LoadBefore,
        LoadAfter
    }

    public struct ModReference
    {
        public RimMod Mod;
        public ModReferenceDirection Direction;
        public bool Forced;

        public ModReference(RimMod mod, ModReferenceDirection direction, bool forced)
        {
            Mod = mod;
            Direction = direction;
            Forced = forced;
        }

        public static ModReference BuildRef(string id, IReadOnlyDictionary<string, RimMod> packageIdToMod, ModReferenceDirection direction, bool forced)
        {
            if (!packageIdToMod.TryGetValue(id, out var dep))
            {
                dep = RimMod.CreateUnknown(id);
            }
            return new ModReference(dep, direction, forced);
        }

        public static ModReference BuildRef(long id, IReadOnlyDictionary<long, RimMod> steamIdToMod, ModReferenceDirection direction, bool forced)
        {
            if (!steamIdToMod.TryGetValue(id, out var dep))
            {
                dep = RimMod.CreateUnknown("unknown.package.id");
                dep.SteamId = id;
            }
            return new ModReference(dep, direction, forced);
        }
    }
}