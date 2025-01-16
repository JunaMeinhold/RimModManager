namespace RimModManager.RimWorld.Sorting
{
    public class GraphCycleException : Exception
    {
        public GraphCycleException()
        {
        }

        public GraphCycleException(string? message) : base(message)
        {
        }

        public GraphCycleException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}