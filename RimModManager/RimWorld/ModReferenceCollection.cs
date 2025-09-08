namespace RimModManager.RimWorld
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ModReferenceCollection : IEnumerable<ModReference>, ICollection<ModReference>
    {
        private readonly Dictionary<ModReferenceSource, HashSet<ModReference>> references = [];
        private int count;

        public ModReferenceCollection(IEnumerable<ModReference> references) : this()
        {
            AddRange(references);
        }

        public ModReferenceCollection()
        {
        }

        public int Count => count;

        public bool IsReadOnly { get; }

        public IReadOnlySet<ModReference> this[ModReferenceSource index]
        {
            get
            {
                if (!references.TryGetValue(index, out var referencesList))
                {
                    referencesList = [];
                    references.Add(index, referencesList);
                }
                return referencesList;
            }
        }

        public void Add(ModReference reference)
        {
            if (!references.TryGetValue(reference.Source, out var referencesList))
            {
                referencesList = [];
                references.Add(reference.Source, referencesList);
            }

            referencesList.Add(reference);
            count++;
        }

        public void AddRange(IEnumerable<ModReference> references)
        {
            foreach (var reference in references)
            {
                Add(reference);
            }
        }

        public void Clear()
        {
            references.Clear();
            count = 0;
        }

        public bool Remove(ModReference reference)
        {
            if (!references.TryGetValue(reference.Source, out var referencesList))
            {
                return false;
            }

            if (referencesList.Remove(reference))
            {
                count--;
                return true;
            }
            return false;
        }

        public bool Contains(ModReference reference)
        {
            if (!references.TryGetValue(reference.Source, out var referencesList))
            {
                return false;
            }

            return referencesList.Contains(reference);
        }

        public IEnumerator<ModReference> GetEnumerator()
        {
            foreach (var pair in references)
            {
                foreach (var reference in pair.Value)
                {
                    yield return reference;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(ModReference[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
    }
}