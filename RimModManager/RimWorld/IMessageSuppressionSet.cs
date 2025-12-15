namespace RimModManager.RimWorld
{
    public interface IMessageSuppressionSet
    {
        bool IsSuppressed(MessageId messageId, string modId, string? targetModId = null);
    }
}