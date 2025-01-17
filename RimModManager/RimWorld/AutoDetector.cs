namespace RimModManager.RimWorld
{
    using System;

    public static class RimPathAutoDetector
    {
        private static readonly string LocalLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) + "Low";

        public static bool Detect(RimModManagerConfig config)
        {
            string logFilePath = Path.Combine(LocalLow, @"Ludeon Studios\RimWorld by Ludeon Studios\Player.log");

            if (File.Exists(logFilePath))
            {
                using var fs = File.Open(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);

                string? line = null;
                while ((line = reader.ReadLine()) != null)
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
                            if (!Directory.Exists(configFolderPath))
                            {
                                return false;
                            }
                            config.GameConfigFolder = configFolderPath.NormalizePath();

                            string steamModFolderPath = GetSteamModFolder(basePath);
                            if (!Directory.Exists(steamModFolderPath))
                            {
                                return false;
                            }
                            config.SteamModFolder = steamModFolderPath.NormalizePath();

                            return true;
                        }

                        return false;
                    }
                }
            }

            return false;
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