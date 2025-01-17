namespace RimModManager.RimWorld
{
    using Hexa.NET.Logging;
    using System;
    using System.Diagnostics;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class RimModManagerConfig
    {
        public static readonly string DefaultConfigPath = "config.json";
        private string? localModsFolder;

        public string? GameFolder { get; set; }

        public string? GameConfigFolder { get; set; }

        public string? SteamModFolder { get; set; }

        [JsonIgnore]
        public string? LocalModsFolder => localModsFolder ??= GameFolder == null ? null : Path.Combine(GameFolder!, "Mods");

        public bool CheckPaths()
        {
            if (!Directory.Exists(GameFolder))
            {
                return false;
            }

            if (!Directory.Exists(GameConfigFolder))
            {
                return false;
            }

            if (!Directory.Exists(SteamModFolder))
            {
                return false;
            }

            if (!File.Exists(Path.Combine(GameFolder, "RimWorldWin64.exe")))
            {
                return false;
            }

            if (!File.Exists(Path.Combine(GameConfigFolder, "ModsConfig.xml")))
            {
                return false;
            }

            return true;
        }

        public static RimModManagerConfig Load(out bool isNew)
        {
            return Load(DefaultConfigPath, out isNew);
        }

        public static RimModManagerConfig Load(string path, out bool isNew)
        {
            if (!File.Exists(path))
            {
                isNew = true;
                return new();
            }

            isNew = false;
            using var fs = File.OpenRead(path);
            RimModManagerConfig? config = (RimModManagerConfig?)JsonSerializer.Deserialize(fs, typeof(RimModManagerConfig), RimModManagerConfigGenerationContext.Default);
            if (config == null)
            {
                config = new();
                isNew = true;
            }

            return config;
        }

        public void Save()
        {
            Save(DefaultConfigPath);
        }

        public void Save(string path)
        {
            using var fs = File.Create(path);
            JsonSerializer.Serialize(fs, this, typeof(RimModManagerConfig), RimModManagerConfigGenerationContext.Default);
        }

        public void LaunchGame()
        {
            if (!Directory.Exists(GameFolder)) return;
            var gameAppPath = Path.Combine(GameFolder, "RimWorldWin64.exe");
            if (!File.Exists(gameAppPath)) return;
            try
            {
                ProcessStartInfo processStartInfo = new(gameAppPath);
                processStartInfo.UseShellExecute = true;
                processStartInfo.CreateNoWindow = false;
                processStartInfo.WindowStyle = ProcessWindowStyle.Normal;

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                LoggerFactory.General.Error("Failed to launch game.");
                LoggerFactory.General.Log(ex);
            }
        }
    }
}