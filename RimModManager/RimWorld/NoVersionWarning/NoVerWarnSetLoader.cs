namespace RimModManager.RimWorld.NoVersionWarning
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class NoVerWarnSetLoader
    {
        private static bool updatedSinceStart = false;
        private const string BaseUrl = "https://raw.githubusercontent.com/emipa606/NoVersionWarning/refs/heads/main/";
        private const string FileName = "ModIdsToFix.xml";

        private static readonly List<string> versions = ["1.3", "1.4", "1.5", "1.6"];
        private static readonly Dictionary<RimVersion, NoVerWarnSet> cachedSets = [];
        private static readonly SemaphoreSlim semaphore = new(1);

        public static async Task EnsureUpdated()
        {
            if (Interlocked.Exchange(ref updatedSinceStart, true)) return;

            var config = RimModManagerConfig.Default;
            var now = DateTime.UtcNow;
            if ((now - config.NoVersionWarnLastUpdated).TotalDays < 1)
            {
                return;
            }
            config.NoVersionWarnLastUpdated = now;
            config.Save();

            await UpdateAsync();
        }

        public static async Task UpdateAsync()
        {
            using var guard = await semaphore.LockAsync();
            using HttpClient client = new();
            foreach (string version in versions)
            {
                string url = $"{BaseUrl}{version}/{FileName}";
                string path = Path.Combine(Paths.DatabaseFolder, $"NoVerWarn_{version}.xml");
                await client.DownloadFileAsync(url, path);
            }
            cachedSets.Clear();
        }

        public static async Task<NoVerWarnSet?> GetSetForVersion(RimVersion version)
        {
            await EnsureUpdated();

            using var guard = await semaphore.LockAsync();
            version = version.ToCompareVersion();
            if (!cachedSets.TryGetValue(version, out NoVerWarnSet? result))
            {
                string versionStr = $"{version.Major}.{version.Minor}";
                if (versions.Contains(versionStr))
                {
                    string path = Path.Combine(Paths.DatabaseFolder, $"NoVerWarn_{versionStr}.xml");
                    result = NoVerWarnSet.Load(path);
                }
                else
                {
                    result = null;
                }
                cachedSets[version] = result!;
            }
            return result;
        }
    }
}
