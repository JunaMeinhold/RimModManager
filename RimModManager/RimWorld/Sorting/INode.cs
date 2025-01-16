namespace RimModManager.RimWorld.Sorting
{
    using System.Collections.Generic;

    public interface INode<T> where T : INode<T>
    {
        public IEnumerable<T> Dependencies { get; }
    }
}