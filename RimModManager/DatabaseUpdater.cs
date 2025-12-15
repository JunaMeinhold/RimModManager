namespace RimModManager
{
    using RimModManager.RimWorld;
    using RimModManager.RimWorld.Rules;
    using RimModManager.RimWorld.SteamDB;
    using System.Threading.Tasks;

    public class DatabaseUpdater
    {
        public const string CommunityRulesUrl = "https://raw.githubusercontent.com/JunaMeinhold/RMM.Database/refs/heads/main/database/communityRules.json";
        public const string SteamRulesUrl = "https://raw.githubusercontent.com/JunaMeinhold/RMM.Database/refs/heads/main/database/steamRules.json";

        public static readonly string CommunityRulesPath = Path.Combine(Paths.DatabaseFolder, "communityRules.json");
        public static readonly string SteamRulesPath = Path.Combine(Paths.DatabaseFolder, "steamRules.json");
        public static readonly string CustomRulesPath = Path.Combine(Paths.DatabaseFolder, "customRules.json");

        private static readonly SemaphoreSlim semaphore = new(1);
        private static bool hasUpdatedSinceStart;

        public static async Task EnsureUpdatedAsync()
        {
            if (Interlocked.Exchange(ref hasUpdatedSinceStart, true)) return;

            var config = RimModManagerConfig.Default;
            var now = DateTime.UtcNow;
            if ((now - config.DatabaseLastUpdated).TotalDays < 1)
            {
                return;
            }
            config.DatabaseLastUpdated = now;
            config.Save();

            await UpdateAsync();
        }

        public static async Task UpdateAsync()
        {
            using var guard = await semaphore.LockAsync();
            using HttpClient client = new();
            await client.DownloadFileAsync(CommunityRulesUrl, CommunityRulesPath); 
            RuleSet.ResetCommunityRules();
            await client.DownloadFileAsync(SteamRulesUrl, SteamRulesPath);
            SteamDatabase.ResetInstance();
        }
    }
}
