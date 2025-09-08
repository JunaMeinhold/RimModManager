namespace RimModManager.RimWorld
{
    using System;

    [Flags]
    public enum ModFlags
    {
        None = 0,
        Git = 1,
        UpdateAvailable = 2,
    }
}