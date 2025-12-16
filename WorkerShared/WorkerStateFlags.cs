namespace WorkerShared
{
    public enum WorkerState : uint
    {
        None = 0,
        Idle = 1,
        WaitingForJobRequest = 2,
        Busy = 3,
        Error = 4,
    }
}