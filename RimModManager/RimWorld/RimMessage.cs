namespace RimModManager.RimWorld
{
    public struct RimMessage
    {
        public RimMod Mod;
        public string Message;
        public RimSeverity Severity;

        public RimMessage(RimMod mod, string message, RimSeverity severity)
        {
            Mod = mod;
            Message = message;
            Severity = severity;
        }
    }
}