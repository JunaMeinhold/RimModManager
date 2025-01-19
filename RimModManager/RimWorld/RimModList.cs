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
        private readonly Lock _lock = new();

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

        public int Count => mods.Count;

        public bool IsReadOnly => false;

        public Lock SyncRoot => _lock;

        public RimMod this[int index]
        {
            get
            {
                lock (_lock)
                {
                    return mods[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    mods[index] = value;
                }
            }
        }

        public void EnterLock()
        {
            _lock.Enter();
        }

        public void ExitLock()
        {
            _lock.Exit();
        }

        public bool TryGetMod(string packageId, [NotNullWhen(true)] out RimMod? mod)
        {
            lock (_lock)
            {
                return packageIdToMod.TryGetValue(packageId, out mod);
            }
        }

        public RimMod? FindMod(string packageId)
        {
            lock (_lock)
            {
                packageIdToMod.TryGetValue(packageId, out var mod);
                return mod;
            }
        }

        public bool Contains(string packageId)
        {
            lock (_lock)
            {
                return modIds.Contains(packageId);
            }
        }

        public bool Contains(long steamId)
        {
            lock (_lock)
            {
                return steamIdToMod.ContainsKey(steamId);
            }
        }

        public int IndexOf(RimMod item)
        {
            lock (_lock)
            {
                return mods.IndexOf(item);
            }
        }

        public void Insert(int index, RimMod item)
        {
            lock (_lock)
            {
                modIds.Add(item.PackageId);
                packageIdToMod[item.PackageId] = item;
                if (item.SteamId.HasValue)
                {
                    steamIdToMod[item.SteamId.Value] = item;
                }
                mods.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                var item = mods[index];

                modIds.Remove(item.PackageId);
                packageIdToMod.Remove(item.PackageId);
                if (item.SteamId.HasValue)
                {
                    steamIdToMod.Remove(item.SteamId.Value);
                }

                mods.RemoveAt(index);
            }
        }

        public void Add(RimMod item)
        {
            lock (_lock)
            {
                modIds.Add(item.PackageId);
                packageIdToMod[item.PackageId] = item;
                if (item.SteamId.HasValue)
                {
                    steamIdToMod[item.SteamId.Value] = item;
                }

                mods.Add(item);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                modIds.Clear();
                packageIdToMod.Clear();
                steamIdToMod.Clear();
                mods.Clear();
            }
        }

        public bool Contains(RimMod item)
        {
            lock (_lock)
            {
                return mods.Contains(item);
            }
        }

        public void CopyTo(RimMod[] array, int arrayIndex)
        {
            lock (_lock)
            {
                mods.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(RimMod item)
        {
            lock (_lock)
            {
                modIds.Remove(item.PackageId);
                packageIdToMod.Remove(item.PackageId);
                if (item.SteamId.HasValue)
                {
                    steamIdToMod.Remove(item.SteamId.Value);
                }

                return mods.Remove(item);
            }
        }

        public RimModList Clone()
        {
            return new(Mods.Select(x => x.Clone()).ToList());
        }

        public IEnumerator<RimMod> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        private struct Enumerator : IEnumerator<RimMod>
        {
            private readonly RimModList mods;
            private int index = -1;
            private RimMod? current;

            public Enumerator(RimModList mods)
            {
                this.mods = mods;
                mods._lock.Enter();
            }

            public readonly RimMod Current => current!;

            readonly object IEnumerator.Current => current!;

            public readonly void Dispose()
            {
                mods._lock.Exit();
            }

            public bool MoveNext()
            {
                index++;
                if (index == mods.Count)
                {
                    return false;
                }
                current = mods[index];
                return true;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
}