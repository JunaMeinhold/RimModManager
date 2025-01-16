namespace RimModManager.RimWorld
{
    using RimModManager.RimWorld.Sorting;
    using System.Collections.Generic;

    public class RimMod : INode<RimMod>
    {
        public ModKind Kind { get; set; }

        public bool IsActive { get; set; }

        public string? Path { get; set; } = null!;

        public long? SteamId { get; set; }

        public ModMetadata Metadata { get; set; } = null!;

        public string Name => Metadata.Name ?? Metadata.PackageId;

        public string PackageId => Metadata.PackageId;

        public string PreviewImagePath => System.IO.Path.Combine(Path ?? string.Empty, "About", "preview.png");

        public List<RimMessage> Messages { get; } = [];

        public bool HasWarnings { get; set; }

        public bool HasErrors { get; set; }

        public List<ModReference> LoadBefore { get; } = [];

        public List<ModReference> LoadAfter { get; } = [];

        IEnumerable<RimMod> INode<RimMod>.Dependencies => Dependencies;

        public bool? LoadBottom { get; set; }

        public List<RimMod> Dependencies = [];
        public List<RimMod> Dependants = [];

        public bool IsMod(string id)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(id, PackageId);
        }

        public void ClearSortingState()
        {
            Dependants.Clear();
            Dependencies.Clear();
        }

        public void AddMessage(string message, RimSeverity severity)
        {
            if (severity == RimSeverity.Warn) HasWarnings = true;
            if (severity == RimSeverity.Error) HasErrors = true;
            RimMessage msg = new(message, severity);
            Messages.Add(msg);
        }

        public void ClearMessages()
        {
            HasWarnings = false;
            HasErrors = false;
            Messages.Clear();
        }

        public static RimMod CreateUnknown(string packageId)
        {
            return new() { Kind = ModKind.Unknown, IsActive = false, Metadata = new() { PackageId = packageId, Name = packageId, Authors = ["Unknown"], Description = "Unknown mod.", SupportedVersions = [] } };
        }

        public override string ToString()
        {
            return $"{Name} ({PackageId})";
        }
    }

    public enum RimSeverity
    {
        Info,
        Warn,
        Error,
    }

    public struct RimMessage
    {
        public string Message;
        public RimSeverity Severity;

        public RimMessage(string message, RimSeverity severity)
        {
            Message = message;
            Severity = severity;
        }
    }
}