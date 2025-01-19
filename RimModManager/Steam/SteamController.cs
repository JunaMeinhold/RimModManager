namespace RimModManager.Steam
{
    using Hexa.NET.KittyUI.Web;
    using Hexa.NET.Logging;
    using Steamworks;
    using System;

    public class SteamController
    {
        private bool initialized;

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly SemaphoreSlim signal = new(0, 1);
        private Task callbackTask;

        private AppId_t appId = new(294100);
        private CallResult<SteamUGCQueryCompleted_t> ugcQueryCompleted;

        public void Init()
        {
            if (initialized)
            {
                return;
            }

            initialized = SteamAPI.Init();

            if (!initialized)
            {
                LoggerFactory.General.Error("Failed to init Steamworks API.");
            }

            callbackTask = Task.Run(() => CallbackTaskLoop(cancellationTokenSource.Token));

            SteamAPI.RunCallbacks();
        }

        private async Task CallbackTaskLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await signal.WaitAsync(cancellationToken);
                SteamAPI.RunCallbacks();
            }
        }

        private void Signal()
        {
            if (signal.CurrentCount == 0)
            {
                signal.Release();
            }
        }

        public void Query()
        {
            ugcQueryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted);

            var query = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByTrend, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items, appId, appId, 1);

            SteamAPICall_t apiCall = SteamUGC.SendQueryUGCRequest(query);

            ugcQueryCompleted.Set(apiCall);
            Signal();
        }

        private void OnUGCQueryCompleted(SteamUGCQueryCompleted_t param, bool bIOFailure)
        {
            if (bIOFailure || param.m_eResult != EResult.k_EResultOK) return;

            for (uint i = 0; i < param.m_unNumResultsReturned; i++)
            {
                // Fetch details for the current item
                SteamUGCDetails_t details;
                if (SteamUGC.GetQueryUGCResult(param.m_handle, i, out details))
                {
                    // Log or process the item details
                    Console.WriteLine($"Item {i + 1}/{param.m_unNumResultsReturned}:");
                    Console.WriteLine($"  Title: {details.m_rgchTitle}");
                    Console.WriteLine($"  PublishedFileId: {details.m_nPublishedFileId.m_PublishedFileId}");
                    Console.WriteLine($"  Description: {details.m_rgchDescription}");
                    Console.WriteLine($"  File Size: {details.m_nFileSize} bytes");
                    Console.WriteLine($"  Creator App ID: {details.m_nCreatorAppID}");
                    Console.WriteLine($"  Consumer App ID: {details.m_nConsumerAppID}");
                    Console.WriteLine($"  Tags: {details.m_rgchTags}");

                    SteamUGC.GetQueryUGCPreviewURL(param.m_handle, i, out var pchURL, 1024);
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve details for item {i + 1}.");
                }
            }

            SteamUGC.ReleaseQueryUGCRequest(param.m_handle);
        }

        public void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            callbackTask?.Wait();

            SteamAPI.Shutdown();
            initialized = false;

            LoggerFactory.General.Info("Steamworks API shutdown complete.");
        }
    }
}