namespace RimModManager.RimWorld.Sorting
{
    using System.Collections.Generic;

    public class TopologicalSorterKahn<T> where T : INode<T>
    {
        private readonly List<T> sortedList = new();
        private readonly Dictionary<T, int> inDegrees = new();
        private readonly Queue<T> zeroInDegreeQueue = new();

        public List<T> TopologicalSort(List<T> nodes)
        {
            sortedList.Clear();
            return TopologicalSort(nodes, sortedList);
        }

        public List<T> TopologicalSort(IEnumerable<T> nodes, List<T> sortedList)
        {
            // Clear previous data
            inDegrees.Clear();
            zeroInDegreeQueue.Clear();

            int nodeCount = 0;
            foreach (T node in nodes)
            {
                inDegrees[node] = 0;
                nodeCount++;
            }

            foreach (T node in nodes)
            {
                foreach (T dependency in node.Dependencies)
                {
                    inDegrees[dependency]++;
                }
            }

            foreach (T node in nodes)
            {
                if (inDegrees[node] == 0)
                {
                    zeroInDegreeQueue.Enqueue(node);
                }
            }

            while (zeroInDegreeQueue.Count > 0)
            {
                T currentNode = zeroInDegreeQueue.Dequeue();
                sortedList.Add(currentNode);

                foreach (T dependency in currentNode.Dependencies)
                {
                    inDegrees[dependency]--;

                    if (inDegrees[dependency] == 0)
                    {
                        zeroInDegreeQueue.Enqueue(dependency);
                    }
                }
            }

            if (sortedList.Count != nodeCount)
            {
                throw new GraphCycleException("The graph contains a cycle.");
            }

            return sortedList;
        }
    }
}