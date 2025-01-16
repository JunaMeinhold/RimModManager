namespace RimModManager.RimWorld
{
    using System;

    public static class RimPathAutoDetector
    {
        private static readonly string LocalLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + "Low";

        public static void Detect(RimModManagerConfig config)
        {
            string logFilePath = Path.Combine(LocalLow, @"Ludeon Studios\RimWorld by Ludeon Studios\Player.log");

            if (File.Exists(logFilePath))
            {
                var lines = File.ReadAllLines(logFilePath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("Mono path[0] ="))
                    {
                        // Extract the path from the log line (e.g., 'D:/SteamLibrary/steamapps/common/RimWorld/RimWorldWin64_Data/Managed')
                        var path = line.AsSpan("Mono path[0] =".Length).Trim().Trim('\'');
                        var basePath = BacktrackPath(path, 2).ToString();

                        if (Directory.Exists(basePath))
                        {
                            config.GameFolder = basePath.NormalizePath();

                            string configFolderPath = Path.Combine(LocalLow, @"Ludeon Studios\RimWorld by Ludeon Studios\Config");
                            if (Directory.Exists(configFolderPath))
                            {
                                config.GameConfigFolder = configFolderPath.NormalizePath();
                            }

                            string steamModFolderPath = GetSteamModFolder(basePath);
                            if (!string.IsNullOrEmpty(steamModFolderPath) && Directory.Exists(steamModFolderPath))
                            {
                                config.SteamModFolder = steamModFolderPath.NormalizePath();
                            }

                            break;
                        }
                    }
                }
            }
        }

        private static unsafe string NormalizePath(this string path)
        {
            // Fix for non conform OS paths.
            char sep = Path.DirectorySeparatorChar;
            fixed (char* pPath = path)
            {
                char* p = pPath;
                char* end = p + path.Length;

                while (p != end)
                {
                    char c = *p;
                    if (c == '\\' || c == '/')
                    {
                        *p = sep;
                    }
                    p++;
                }
            }
            return path;
        }

        private static ReadOnlySpan<char> BacktrackPath(ReadOnlySpan<char> path, int backtack)
        {
            for (int i = 0; i < backtack; i++)
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }

        private static string GetSteamModFolder(string gameFolder)
        {
            var steamAppsPath = BacktrackPath(gameFolder, 2).ToString();

            if (Directory.Exists(steamAppsPath))
            {
                return Path.Combine(steamAppsPath, "workshop", "content", "294100");
            }

            return string.Empty;
        }
    }
}