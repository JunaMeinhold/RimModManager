namespace RimModManager
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Mathematics;
    using RimModManager.RimWorld;

    public class SelectPathDialog : Dialog
    {
        private readonly RimModManagerConfig config;
        private string? errorMessage;

        public SelectPathDialog(RimModManagerConfig config)
        {
            this.config = config;
        }

        public override string Name { get; } = "Select Paths";

        protected override ImGuiWindowFlags Flags { get; } = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDocking;

        protected override void DrawContent()
        {
            var gameFolder = config.GameFolder;
            if (ImGui.InputText("Game Folder"u8, ref gameFolder, 2048))
            {
                config.GameFolder = gameFolder;
            }

            ImGui.SameLine();

            if (ImGui.Button("...##GameFolder"u8))
            {
                OpenFileDialog dialog = new()
                {
                    OnlyAllowFolders = true
                };
                dialog.Show((s, r) =>
                {
                    if (s is not OpenFileDialog dialog || r != DialogResult.Ok) return;
                    config.GameFolder = dialog.SelectedFile!; // is folder in only allow folders.
                }, this, DialogFlags.CenterOnParent);
            }

            var gameConfigFolder = config.GameConfigFolder;
            if (ImGui.InputText("Game Config Folder"u8, ref gameConfigFolder, 2048))
            {
                config.GameConfigFolder = gameConfigFolder;
            }

            ImGui.SameLine();

            if (ImGui.Button("...##GameConfigFolder"u8))
            {
                OpenFileDialog dialog = new()
                {
                    OnlyAllowFolders = true
                };
                dialog.Show((s, r) =>
                {
                    if (s is not OpenFileDialog dialog || r != DialogResult.Ok) return;
                    config.GameConfigFolder = dialog.SelectedFile!; // is folder in only allow folders.
                }, this, DialogFlags.CenterOnParent);
            }

            var steamModFolder = config.SteamModFolder;
            if (ImGui.InputText("Steam Mod Folder"u8, ref steamModFolder, 2048))
            {
                config.SteamModFolder = steamModFolder;
            }

            ImGui.SameLine();

            if (ImGui.Button("...##SteamModFolder"u8))
            {
                OpenFileDialog dialog = new()
                {
                    OnlyAllowFolders = true
                };
                dialog.Show((s, r) =>
                {
                    if (s is not OpenFileDialog dialog || r != DialogResult.Ok) return;
                    config.SteamModFolder = dialog.SelectedFile!; // is folder in only allow folders.
                }, this, DialogFlags.CenterOnParent);
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                ImGui.TextColored(Colors.Crimson, errorMessage);
            }

            if (ImGui.Button("Select"u8))
            {
                errorMessage = string.Empty;

                if (config.CheckPaths())
                {
                    config.Save();
                    Close(DialogResult.Ok);
                }
                else
                {
                    errorMessage = "Invalid paths, please check the selected folders.";
                }
            }
        }
    }
}