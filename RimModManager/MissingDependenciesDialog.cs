namespace RimModManager
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Utilities.Text;
    using RimModManager.RimWorld;

    public class MissingDependenciesDialog : Dialog
    {
        private readonly ModsConfig modsConfig;
        private readonly HashSet<RimMod> missingDependencies;
        private readonly HashSet<RimMod> toActivate = [];

        public MissingDependenciesDialog(ModsConfig modsConfig, HashSet<RimMod> missingDependencies)
        {
            this.modsConfig = modsConfig;
            this.missingDependencies = missingDependencies;
        }

        public override string Name { get; } = "Missing Dependencies";

        protected override ImGuiWindowFlags Flags { get; } = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDocking;

        protected override unsafe void DrawContent()
        {
            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);

            foreach (RimMod mod in missingDependencies)
            {
                builder.Reset();
                builder.Append(mod.Name);
                builder.Append(" ("u8);
                builder.Append(mod.PackageId);
                builder.Append(")"u8);
                builder.End();

                var active = toActivate.Contains(mod);

                if (mod.Kind == ModKind.Unknown)
                {
                    ImGui.Text(builder);
                }
                else
                {
                    if (ImGui.Checkbox(builder, ref active))
                    {
                        if (active)
                        {
                            toActivate.Add(mod);
                        }
                        else
                        {
                            toActivate.Remove(mod);
                        }
                    }
                }

                bool hovered = ImGui.IsItemHovered();

                mod.DrawContextMenu();

                if (hovered)
                {
                    mod.DrawTooltip(builder);
                }
            }

            if (ImGui.Button("Select all & continue"u8))
            {
                modsConfig.ActivateMods(missingDependencies);
                Close(DialogResult.Ok);
            }

            ImGui.SameLine();

            if (ImGui.Button("Select"u8))
            {
                modsConfig.ActivateMods(toActivate);
                Close(DialogResult.Ok);
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel"u8))
            {
                Close(DialogResult.Cancel);
            }
        }
    }
}