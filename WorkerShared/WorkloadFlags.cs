namespace WorkerShared
{
    [Flags]
    public enum WorkloadFlags
    {
        GenerateMips = 1,
        Upscale = 2,
        Downscale = 4,
        FlipVertical = 8,
        Bc7Quick = 16,
    }
}