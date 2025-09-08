namespace RimModManager
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using RimModManager.RimWorld;
    using System.Diagnostics;

    public class SettingsWindow : ImWindow
    {
        private readonly RimModManagerConfig config;

        public SettingsWindow(RimModManagerConfig config)
        {
            this.config = config;
        }

        public override string Name { get; } = "Settings";

        public override void DrawContent()
        {
            if (!ImGui.BeginTabBar("Settings"u8))
            {
                return;
            }

            PageLocations();

            ImGui.EndTabBar();
        }

        private void PageLocations()
        {
            if (!ImGui.BeginTabItem("Locations"u8))
            {
                return;
            }

            ImGui.Text("Game Folder"u8);
            string gameFolder = config.GameFolder ?? string.Empty;
            if (ImGui.InputText("##GameFolder"u8, ref gameFolder, 2048))
            {
                config.GameFolder = gameFolder;
            }
            ImGui.SameLine();
            if (ImGui.Button("...##GameFolderBtn"u8))
            {
                OpenFileDialog dialog = new();
                dialog.OnlyAllowFolders = true;
                dialog.CurrentFolder = gameFolder;
                dialog.Show((s, r) =>
                {
                    if (s is not OpenFileDialog dialog || r != DialogResult.Ok) return;
                    config.GameFolder = dialog.SelectedFile;
                });
            }

            ImGui.Spacing();

            ImGui.Text("Game Config Folder"u8);
            string gameConfigFolder = config.GameConfigFolder ?? string.Empty;
            if (ImGui.InputText("##GameConfigFolder"u8, ref gameConfigFolder, 2048))
            {
                config.GameConfigFolder = gameConfigFolder;
            }
            ImGui.SameLine();
            if (ImGui.Button("...##GameConfigFolderBtn"u8))
            {
                OpenFileDialog dialog = new();
                dialog.OnlyAllowFolders = true;
                dialog.CurrentFolder = gameConfigFolder;
                dialog.Show((s, r) =>
                {
                    if (s is not OpenFileDialog dialog || r != DialogResult.Ok) return;
                    config.GameConfigFolder = dialog.SelectedFile;
                });
            }

            ImGui.Spacing();

            ImGui.Text("Steam Mod Folder"u8);
            string steamModFolder = config.SteamModFolder ?? string.Empty;
            if (ImGui.InputText("##SteamModFolder"u8, ref steamModFolder, 2048))
            {
                config.SteamModFolder = steamModFolder;
            }
            ImGui.SameLine();
            if (ImGui.Button("...##SteamModFolderBtn"u8))
            {
                OpenFileDialog dialog = new();
                dialog.OnlyAllowFolders = true;
                dialog.CurrentFolder = steamModFolder;
                dialog.Show((s, r) =>
                {
                    if (s is not OpenFileDialog dialog || r != DialogResult.Ok) return;
                    config.SteamModFolder = dialog.SelectedFile;
                });
            }

            ImGui.EndTabItem();
        }
    }
}