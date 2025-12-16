namespace RimModManager
{
    using Hexa.NET.Utilities;
    using System;

    public static unsafe class Paths
    {
        public static string AppDataFolder { get; }

        public static string ProfilesFolder { get; }

        public static string DatabaseFolder { get; }

        public static string LogFolder { get; }

        public static byte* ImGuiIniFile { get; }

        private static void EnsureCreatedDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        static Paths()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AppDataFolder = Path.Combine(appData, "RimModManager");
            EnsureCreatedDirectory(AppDataFolder);
            ProfilesFolder = Path.Combine(AppDataFolder, "profiles");
            EnsureCreatedDirectory(ProfilesFolder);
            DatabaseFolder = Path.Combine(AppDataFolder, "database");
            EnsureCreatedDirectory(DatabaseFolder);
            LogFolder = Path.Combine(AppDataFolder, "logs");
            EnsureCreatedDirectory(LogFolder);

            string imGuiIniPath = Path.Combine(AppDataFolder, "imgui.ini");
            ImGuiIniFile = imGuiIniPath.ToUTF8Ptr();
        }
    }
}