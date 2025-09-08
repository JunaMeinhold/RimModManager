namespace RimModManager
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Utilities.Text;
    using RimModManager.RimWorld;
    using System;
    using System.Numerics;

    public class ModRuleDialog : Dialog
    {
        private readonly RimMod mod;
        private readonly RimLoadOrder loadOrder;
        private readonly RimModList modList;
        private float width = 0;

        public ModRuleDialog(RimMod mod, RimLoadOrder loadOrder)
        {
            this.mod = mod;
            this.loadOrder = loadOrder;
            modList = loadOrder.ModList;
        }

        public override string Name { get; } = "Rules";

        protected override ImGuiWindowFlags Flags { get; }

        protected override unsafe void DrawContent()
        {
            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);

            ImGui.SeparatorText(mod.Name);

            var avail = ImGui.GetContentRegionAvail();

            DrawLoadAfter(ref builder, avail);
            ImGuiSplitter.VerticalSplitter("S"u8, ref width);
            DrawLoadBefore(ref builder);
        }

        private unsafe void DrawLoadBefore(ref StrBuilder builder)
        {
            if (!ImGui.BeginChild("LoadBefore"u8))
            {
                ImGui.EndChild();
                return;
            }
            ImGui.Text("Load Before"u8);

            int id = 0;

            DrawMods("Metadata"u8, mod.LoadBefore[ModReferenceSource.ModMetadata], ref id, ref builder);
            DrawMods("Steam"u8, mod.LoadBefore[ModReferenceSource.SteamRules], ref id, ref builder);
            DrawMods("Community"u8, mod.LoadBefore[ModReferenceSource.CommunityRules], ref id, ref builder);
            DrawMods("Custom"u8, mod.LoadBefore[ModReferenceSource.CustomRules], ref id, ref builder);

            ImGui.EndChild();
            return;
        }

        private unsafe void DrawLoadAfter(ref StrBuilder builder, Vector2 avail)
        {
            if (!ImGui.BeginChild("LoadAfter"u8, new Vector2(avail.X * 0.5f + width, avail.Y)))
            {
                ImGui.EndChild();
                return;
            }
            ImGui.Text("Load After"u8);

            int id = 0;

            DrawMods("Metadata"u8, mod.LoadAfter[ModReferenceSource.ModMetadata], ref id, ref builder);
            DrawMods("Steam"u8, mod.LoadAfter[ModReferenceSource.SteamRules], ref id, ref builder);
            DrawMods("Community"u8, mod.LoadAfter[ModReferenceSource.CommunityRules], ref id, ref builder);
            DrawMods("Custom"u8, mod.LoadAfter[ModReferenceSource.CustomRules], ref id, ref builder);

            ImGui.EndChild();
            return;
        }

        private static unsafe void DrawMods(ReadOnlySpan<byte> label, IReadOnlySet<ModReference> set, ref int id, ref StrBuilder builder)
        {
            if (!ImGui.BeginListBox(label))
            {
                return;
            }
            foreach (var item in set)
            {
                var mod = item.Mod;
                ImGui.Selectable(mod.BuildLabel(builder, id));
                mod.DrawTooltip(builder);
                id++;
            }
            ImGui.EndListBox();
        }
    }
}