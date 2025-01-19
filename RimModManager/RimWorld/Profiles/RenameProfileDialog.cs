namespace RimModManager.RimWorld.Profiles
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Mathematics;

    public class RenameProfileDialog : Dialog
    {
        private readonly RimProfileManager profileManager;
        private readonly RimProfile profile;
        private string newProfileName = "New Profile";
        private string? message;

        public RenameProfileDialog(RimProfileManager profileManager, RimProfile profile)
        {
            this.profileManager = profileManager;
            this.profile = profile;
        }

        public RimProfileManager ProfileManager => profileManager;

        public RimProfile Profile => profile;

        public string NewProfileName => newProfileName;

        public override string Name { get; } = "Rename Profile";

        protected override ImGuiWindowFlags Flags { get; } = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDocking;

        protected override void DrawContent()
        {
            if (ImGui.InputText("##Name"u8, ref newProfileName, 255, ImGuiInputTextFlags.AutoSelectAll))
            {
                ValidateProfileName();
            }

            if (message != null)
            {
                ImGui.TextColored(Colors.Crimson, message);
            }

            if (ImGui.Button("Cancel"u8))
            {
                Close(DialogResult.Cancel);
            }
            ImGui.SameLine();
            if (ImGui.Button("Rename"u8) || ImGuiP.IsKeyPressed(ImGuiKey.Enter))
            {
                if (ValidateProfileName())
                {
                    Close(DialogResult.Ok);
                }
            }

            if (ImGuiP.IsKeyPressed(ImGuiKey.Escape))
            {
                Close(DialogResult.Cancel);
            }
        }

        private bool ValidateProfileName()
        {
            if (string.IsNullOrWhiteSpace(newProfileName))
            {
                message = "Profile name cannot be empty.";
                return false;
            }

            if (profileManager.Contains(newProfileName) && profile.Name != newProfileName)
            {
                message = "Profile already exists.";
                return false;
            }

            message = null;
            return true;
        }
    }
}