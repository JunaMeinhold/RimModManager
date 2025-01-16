namespace RimModManager.RimWorld.Sorting
{
    using System.Collections.Generic;

    public class TopologicalSorterDFS<T> where T : INode<T>
    {
        private readonly List<T> sortedList = [];
        private readonly HashSet<T> visitedNodes = [];
        private readonly HashSet<T> recursionStack = [];
        private readonly Stack<T> cycleStack = new();

        public List<T> TopologicalSort(List<T> nodes)
        {
            sortedList.Clear();
            return TopologicalSort(nodes, sortedList);
        }

        public List<T> TopologicalSort(IEnumerable<T> nodes, List<T> sortedList)
        {
            visitedNodes.Clear();
            recursionStack.Clear();
            cycleStack.Clear();

            foreach (T node in nodes)
            {
                if (!visitedNodes.Contains(node))
                {
                    if (!TopologicalSortRecursive(node, sortedList))
                    {
                        throw new GraphCycleException("The graph contains a cycle: " + GetCyclePath());
                    }
                }
            }

            return sortedList;
        }

        private bool TopologicalSortRecursive(T node, List<T> sortedList)
        {
            visitedNodes.Add(node);
            recursionStack.Add(node);
            cycleStack.Push(node);

            foreach (T dependency in node.Dependencies)
            {
                if (!visitedNodes.Contains(dependency))
                {
                    if (!TopologicalSortRecursive(dependency, sortedList))
                    {
                        return false;
                    }
                }
                else if (recursionStack.Contains(dependency))
                {
                    cycleStack.Push(dependency);
                    // A cycle is detected
                    return false;
                }
            }

            sortedList.Add(node);
            recursionStack.Remove(node);
            cycleStack.Pop();

            return true;
        }

        private string GetCyclePath()
        {
            if (cycleStack.Count == 0)
            {
                return string.Empty;
            }

            var cyclePath = cycleStack.Reverse().Select(node => node.ToString());
            return string.Join(" -> ", cyclePath);
        }
    }
}