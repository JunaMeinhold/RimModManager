namespace RimModManager.Steam
{
    using Hexa.NET.Logging;
    using Steamworks;

    public class SteamController
    {
        private bool initialized;

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly SemaphoreSlim signal = new(0, 1);
        private Task callbackTask;

        private AppId_t appId = new(294100);

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

        public void SubscribeToWorkshopItem(ulong fileId)
        {
            if (!initialized)
            {
                return;
            }

            SteamUGC.SubscribeItem(new(fileId));
        }
        public void UnsubscribeToWorkshopItem(ulong fileId)
        {
            if (!initialized)
            {
                return;
            }

            SteamUGC.UnsubscribeItem(new(fileId));
        }

        public bool IsWorkshopItemSubscribed(ulong fileId)
        {
            if (!initialized)
            {
                return false;
            }

            uint itemState = SteamUGC.GetItemState(new(fileId));

            bool isSubscribed = (itemState & (uint)EItemState.k_EItemStateSubscribed) != 0;
            return isSubscribed;
        }

        private void Signal()
        {
            if (signal.CurrentCount == 0)
            {
                signal.Release();
            }
        }

        public void Dispose()
        {
            if (!initialized)
            {
                return;
            }

            cancellationTokenSource.Cancel();
            try
            {
                callbackTask?.Wait();
            }
            catch
            {
            }

            SteamAPI.Shutdown();
            initialized = false;

            LoggerFactory.General.Info("Steamworks API shutdown complete.");
        }
    }
}