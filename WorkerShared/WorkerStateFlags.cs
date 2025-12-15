namespace WorkerShared
{
    [Flags]
    public enum WorkerState : uint
    {
        None = 0,
        Idle = 1,
        WaitingForJobRequest = 2,
        Busy = 4,
        Error = 8,
    }
}