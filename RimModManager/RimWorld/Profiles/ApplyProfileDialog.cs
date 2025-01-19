namespace RimModManager.RimWorld.Profiles
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Utilities.Text;
    using System.Numerics;

    public class ApplyProfileDialog : Dialog
    {
        private readonly RimProfile profile;
        private readonly RimLoadOrder loadOrder;
        private readonly RimModList mods;

        public ApplyProfileDialog(RimProfile profile, RimLoadOrder loadOrder, RimModList mods)
        {
            this.profile = profile;
            this.loadOrder = loadOrder;
            this.mods = mods;
            profile.PopulateList(mods);
        }

        public override string Name { get; } = "Apply Profile";

        protected override ImGuiWindowFlags Flags { get; } = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDocking;

        protected override unsafe void DrawContent()
        {
            ImGui.BeginChild("##ModList"u8, new Vector2(500, 600));
            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);

            profile.DrawLoadOrder(builder);
            ImGui.EndChild();
            if (ImGui.Button("Cancel"u8))
            {
                Close(DialogResult.Cancel);
            }
            ImGui.SameLine();
            if (ImGui.Button("Apply"u8))
            {
                loadOrder.Apply(profile, mods);
                Close(DialogResult.Ok);
            }
        }
    }
}