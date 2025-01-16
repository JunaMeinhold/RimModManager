namespace RimModManager
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class FilteredList<T> : IReadOnlyList<T>
    {
        private readonly List<T> innerList = [];
        private readonly IReadOnlyList<T> baseList;
        private readonly Func<T, bool> selector;

        public FilteredList(IReadOnlyList<T> baseList, Func<T, bool> selector)
        {
            this.baseList = baseList;
            this.selector = selector;
            Refresh();
        }

        public T this[int index] => innerList[index];

        public int Count => innerList.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        public void Refresh()
        {
            innerList.Clear();
            foreach (var item in baseList)
            {
                if (selector(item))
                {
                    innerList.Add(item);
                }
            }
        }
    }
}