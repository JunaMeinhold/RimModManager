namespace RimModManager.RimWorld
{
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;

    public class RimModList : IList<RimMod>
    {
        private readonly List<RimMod> mods = [];
        private readonly HashSet<string> modIds = [];
        private readonly Dictionary<string, RimMod> packageIdToMod = [];
        private readonly Dictionary<long, RimMod> steamIdToMod = [];

        public RimModList(List<RimMod> mods)
        {
            this.mods = mods;
            modIds = mods.Select(x => x.PackageId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            packageIdToMod = new(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in mods)
            {
                packageIdToMod[mod.PackageId] = mod;
                if (mod.SteamId.HasValue)
                {
                    steamIdToMod[mod.SteamId.Value] = mod;
                }
            }
        }

        public IReadOnlyList<RimMod> Mods => mods;

        public IReadOnlySet<string> ModIds => modIds;

        public IReadOnlyDictionary<string, RimMod> PackageIdToMod => packageIdToMod;

        public IReadOnlyDictionary<long, RimMod> SteamIdToMod => steamIdToMod;

        public int Count => ((ICollection<RimMod>)mods).Count;

        public bool IsReadOnly => ((ICollection<RimMod>)mods).IsReadOnly;

        public RimMod this[int index] { get => ((IList<RimMod>)mods)[index]; set => ((IList<RimMod>)mods)[index] = value; }

        public bool TryGetMod(string packageId, [NotNullWhen(true)] out RimMod? mod)
        {
            return packageIdToMod.TryGetValue(packageId, out mod);
        }

        public RimMod? FindMod(string packageId)
        {
            packageIdToMod.TryGetValue(packageId, out var mod);
            return mod;
        }

        public bool Contains(string packageId)
        {
            return modIds.Contains(packageId);
        }

        public int IndexOf(RimMod item)
        {
            return ((IList<RimMod>)mods).IndexOf(item);
        }

        public void Insert(int index, RimMod item)
        {
            ((IList<RimMod>)mods).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<RimMod>)mods).RemoveAt(index);
        }

        public void Add(RimMod item)
        {
            ((ICollection<RimMod>)mods).Add(item);
        }

        public void Clear()
        {
            ((ICollection<RimMod>)mods).Clear();
        }

        public bool Contains(RimMod item)
        {
            return ((ICollection<RimMod>)mods).Contains(item);
        }

        public void CopyTo(RimMod[] array, int arrayIndex)
        {
            ((ICollection<RimMod>)mods).CopyTo(array, arrayIndex);
        }

        public bool Remove(RimMod item)
        {
            return ((ICollection<RimMod>)mods).Remove(item);
        }

        public IEnumerator<RimMod> GetEnumerator()
        {
            return ((IEnumerable<RimMod>)mods).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)mods).GetEnumerator();
        }
    }
}