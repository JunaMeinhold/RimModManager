namespace RimModManager.RimWorld.Profiles
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Mathematics;

    public class CreateProfileDialog : Dialog
    {
        private readonly RimProfileManager profileManager;
        private string profileName = "New Profile";
        private string? message;

        public CreateProfileDialog(RimProfileManager profileManager)
        {
            this.profileManager = profileManager;
        }

        public RimProfileManager ProfileManager => profileManager;

        public string ProfileName => profileName;

        public override string Name { get; } = "Create Profile";

        protected override ImGuiWindowFlags Flags { get; } = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDocking;

        protected override void DrawContent()
        {
            if (ImGui.InputText("##Name"u8, ref profileName, 255, ImGuiInputTextFlags.AutoSelectAll))
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
            if (ImGui.Button("Create"u8) || ImGuiP.IsKeyPressed(ImGuiKey.Enter))
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
            if (string.IsNullOrWhiteSpace(profileName))
            {
                message = "Profile name cannot be empty.";
                return false;
            }

            if (profileManager.Contains(profileName))
            {
                message = "Profile already exists.";
                return false;
            }

            message = null;
            return true;
        }
    }
}