namespace RimModManager.RimWorld
{
    using System.Collections.Generic;

    public struct ModReference : IEquatable<ModReference>
    {
        public RimMod Mod;
        public ModReferenceDirection Direction;
        public ModReferenceSource Source;
        public bool Forced;

        public ModReference(RimMod mod, ModReferenceDirection direction, ModReferenceSource source, bool forced)
        {
            Mod = mod;
            Direction = direction;
            Source = source;
            Forced = forced;
        }

        public static ModReference BuildRef(string id, IReadOnlyDictionary<string, RimMod> packageIdToMod, ModReferenceDirection direction, ModReferenceSource source, bool forced)
        {
            if (!packageIdToMod.TryGetValue(id, out var dep))
            {
                dep = RimMod.CreateUnknown(id);
            }
            return new ModReference(dep, direction, source, forced);
        }

        public static ModReference BuildRef(long id, IReadOnlyDictionary<long, RimMod> steamIdToMod, ModReferenceDirection direction, ModReferenceSource source, bool forced)
        {
            if (!steamIdToMod.TryGetValue(id, out var dep))
            {
                dep = RimMod.CreateUnknown("unknown.package.id");
                dep.SteamId = id;
            }
            return new ModReference(dep, direction, source, forced);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is ModReference reference && Equals(reference);
        }

        public readonly bool Equals(ModReference other)
        {
            return Mod.PackageId.Equals(other.Mod.PackageId, StringComparison.OrdinalIgnoreCase) &&
                   Direction == other.Direction;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Mod.PackageId.GetHashCode(StringComparison.OrdinalIgnoreCase), Direction);
        }

        public static bool operator ==(ModReference left, ModReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ModReference left, ModReference right)
        {
            return !(left == right);
        }
    }
}