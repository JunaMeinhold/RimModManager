namespace RimModManager.RimWorld
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public enum MessageId
    {
        Unknown,
        ModIncompatible,
        ModGameVersion,
        ModMissingDependency,
        ModLoadBefore,
        ModLoadAfter,
    }

    public struct MessageSuppressionEntry : IEquatable<MessageSuppressionEntry>
    {
        public MessageId MessageId { get; set; }

        public string ModId { get; set; }

        public string? TargetModId { get; set; }

        public MessageSuppressionEntry(MessageId messageId, string modId, string? targetModId = null)
        {
            MessageId = messageId;
            ModId = modId;
            TargetModId = targetModId;
        }

        public readonly override bool Equals(object? obj) => obj is MessageSuppressionEntry entry && Equals(entry);

        public readonly bool Equals(MessageSuppressionEntry other) => MessageId == other.MessageId && ModId == other.ModId && TargetModId == other.TargetModId;

        public override readonly int GetHashCode() => HashCode.Combine(MessageId, ModId, TargetModId);

        public static bool operator ==(MessageSuppressionEntry left, MessageSuppressionEntry right) => left.Equals(right);

        public static bool operator !=(MessageSuppressionEntry left, MessageSuppressionEntry right) => !(left == right);
    }

    public class MessageSuppressionSet : IMessageSuppressionSet
    {
        private readonly HashSet<MessageSuppressionEntry> entries = [];

        public static readonly IMessageSuppressionSet Empty = new MessageSuppressionSet();

        public HashSet<MessageSuppressionEntry> Entries => entries;

        public void Suppress(MessageId messageId, string modId, string? targetModId = null)
        {
            entries.Add(new MessageSuppressionEntry(messageId, modId, targetModId));
        }

        public bool IsSuppressed(MessageId messageId, string modId, string? targetModId = null)
        {
            return entries.Contains(new MessageSuppressionEntry(messageId, modId, targetModId));
        }

        public void Clear()
        {
            entries.Clear();
        }

        public void Remove(MessageId messageId, string modId, string? targetModId = null)
        {
            entries.Remove(new MessageSuppressionEntry(messageId, modId, targetModId));
        }

        public void Save(Stream stream)
        {
            JsonSerializer.Serialize(stream, this, MessageSuppressionSetGenerationContext.Default.MessageSuppressionSet);
        }

        public void Save(string path)
        {
            using var fs = File.Create(path);
            Save(fs);
        }

        public static MessageSuppressionSet Load(Stream stream)
        {
            return JsonSerializer.Deserialize(stream, MessageSuppressionSetGenerationContext.Default.MessageSuppressionSet) ?? new();
        }

        public static MessageSuppressionSet Load(string path)
        {
            MessageSuppressionSet set;
            if (!File.Exists(path))
            {
                set = new();
            }
            else
            {
                using FileStream fs = File.OpenRead(path);
                set = Load(fs); 
            }

            set.Save(path);
            return set;
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(MessageSuppressionSet))]
    public partial class MessageSuppressionSetGenerationContext : JsonSerializerContext
    {
    }

    public class MergedMessageSuppressionSet : IMessageSuppressionSet
    {
        private readonly List<IMessageSuppressionSet> sets;

        public MergedMessageSuppressionSet(params IMessageSuppressionSet[] sets)
        {
            this.sets = [.. sets];
        }

        public static readonly MergedMessageSuppressionSet Global = new();

        public void AddSet(IMessageSuppressionSet set)
        {
            sets.Add(set);
        }

        public bool IsSuppressed(MessageId messageId, string modId, string? targetModId = null)
        {
            foreach (var set in sets)
            {
                if (set.IsSuppressed(messageId, modId, targetModId))
                {
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            sets.Clear();
        }
    }
}
