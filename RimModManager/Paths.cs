namespace RimModManager
{
    using System;

    public static class Paths
    {
        public static string AppDataFolder { get; }

        public static string ProfilesFolder { get; }

        public static string DatabaseFolder { get; }

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
        }
    }
}
