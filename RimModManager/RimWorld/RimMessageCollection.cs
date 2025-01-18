namespace RimModManager.RimWorld
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities.Text;
    using System.Collections;
    using System.Collections.Generic;

    public class RimMessageCollection : IList<RimMessage>
    {
        private readonly List<RimMessage> messages = [];

        public RimMessageCollection()
        {
        }

        public RimMessageCollection(IEnumerable<RimMessage> messages)
        {
            foreach (var message in messages)
            {
                Add(message);
            }
        }

        public RimMessage this[int index] { get => messages[index]; set => messages[index] = value; }

        public int Count => messages.Count;

        public bool IsReadOnly => ((ICollection<RimMessage>)messages).IsReadOnly;

        public int WarningsCount;

        public int ErrorsCount;

        internal bool AddMessagesToMods;

        public unsafe void DrawBar(StrBuilder builder)
        {
            if (WarningsCount > 0)
            {
                builder.Reset();
                builder.Append(FontAwesome.Warning);
                builder.Append(WarningsCount);
                builder.End();
                ImGui.TextColored(Colors.Yellow, builder);
                DisplayMessages(builder, RimSeverity.Warn);
            }

            if (ErrorsCount > 0)
            {
                if (WarningsCount > 0)
                {
                    ImGui.SameLine();
                }

                builder.Reset();
                builder.Append(FontAwesome.CircleExclamation);
                builder.Append(ErrorsCount);
                builder.End();
                ImGui.TextColored(Colors.Red, builder);
                DisplayMessages(builder, RimSeverity.Error);
            }
        }

        private unsafe void DisplayMessages(StrBuilder builder, RimSeverity severity)
        {
            if (ImGui.BeginItemTooltip())
            {
                foreach (var msg in messages)
                {
                    if (msg.Severity != severity) continue;
                    ImGui.Text(BuildModLabel(builder, msg.Mod));
                    ImGui.Indent();
                    ImGui.Text(msg.Message);
                    ImGui.Unindent();
                }
                ImGui.EndTooltip();
            }
        }

        private static unsafe byte* BuildModLabel(StrBuilder builder, RimMod mod)
        {
            builder.Reset();
            builder.Append(mod.Name);
            builder.End();
            return builder;
        }

        public void Add(RimMessage item)
        {
            if (item.Severity == RimSeverity.Warn) WarningsCount++;
            if (item.Severity == RimSeverity.Error) ErrorsCount++;
            messages.Add(item);
        }

        public void AddMessage(RimMod mod, string message, RimSeverity severity)
        {
            if (severity == RimSeverity.Warn) WarningsCount++;
            if (severity == RimSeverity.Error) ErrorsCount++;
            RimMessage msg = new(mod, message, severity);
            messages.Add(msg);
            if (AddMessagesToMods)
            {
                mod.AddMessage(message, severity);
            }
        }

        public void Clear()
        {
            WarningsCount = 0;
            ErrorsCount = 0;
            messages.Clear();
        }

        public bool Contains(RimMessage item)
        {
            return messages.Contains(item);
        }

        public void CopyTo(RimMessage[] array, int arrayIndex)
        {
            messages.CopyTo(array, arrayIndex);
        }

        public IEnumerator<RimMessage> GetEnumerator()
        {
            return messages.GetEnumerator();
        }

        public int IndexOf(RimMessage item)
        {
            return messages.IndexOf(item);
        }

        public void Insert(int index, RimMessage item)
        {
            if (item.Severity == RimSeverity.Warn) WarningsCount++;
            if (item.Severity == RimSeverity.Error) ErrorsCount++;
            messages.Insert(index, item);
        }

        public bool Remove(RimMessage item)
        {
            if (item.Severity == RimSeverity.Warn) WarningsCount--;
            if (item.Severity == RimSeverity.Error) ErrorsCount--;
            return messages.Remove(item);
        }

        public void RemoveAt(int index)
        {
            var item = messages[index];
            if (item.Severity == RimSeverity.Warn) WarningsCount++;
            if (item.Severity == RimSeverity.Error) ErrorsCount++;
            messages.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return messages.GetEnumerator();
        }
    }
}