namespace RimModManager
{
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.Logging;
    using LibGit2Sharp;
    using RimModManager.RimWorld;
    using System;
    using System.Collections.Concurrent;

    public enum CheckForUpdateResult
    {
        UpToDate = 0,
        UpdateAvailable = 1,
        Failure = 2,
        Busy = 3,
        Canceled = 4,
    }

    public class RimModUpdater
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(nameof(RimModUpdater));
        private readonly ConcurrentQueue<RimMod> queue = new();
        private CancellationToken cancellationToken;
        private bool isBusy;
        private int availableUpdates = -1;
        private int processed = 0;
        private int totalTasks = 0;

        public int MaxConcurrentTasks { get; set; } = 8;

        public async Task<CheckForUpdateResult> CheckForUpdatesAsync(RimModList mods, IProgress<float>? progress = null, CancellationToken token = default)
        {
            if (isBusy) return CheckForUpdateResult.Busy;
            isBusy = true;
            processed = 0;

            try
            {
                cancellationToken = token;
                foreach (var mod in mods)
                {
                    if (mod.IsLocalMod)
                    {
                        if ((mod.Flags & ModFlags.Git) != 0)
                        {
                            queue.Enqueue(mod);
                        }
                    }
                }
                totalTasks = queue.Count;
                Task<FetchResult>[] tasks = new Task<FetchResult>[MaxConcurrentTasks];

                for (int i = 0; i < MaxConcurrentTasks; ++i)
                {
                    tasks[i] = Task.Run(CheckForUpdatesTaskVoid, token);
                }

                Task? updateTicker = null;
                if (progress != null)
                {
                    updateTicker = Task.Run(async () =>
                   {
                       int p;
                       do
                       {
                           p = Volatile.Read(ref processed);
                           progress.Report(p / (float)totalTasks);
                           await Task.Delay(100);
                       } while (p != totalTasks && !token.IsCancellationRequested);
                   }, token);
                }

                int failedToFetch = 0;
                for (int i = 0; i < MaxConcurrentTasks; ++i)
                {
                    var result = await tasks[i];
                    availableUpdates += result.UpdatesFound;
                    failedToFetch += result.FailedToFetch;
                }

                if (updateTicker != null)
                {
                    await updateTicker;
                }

                if (failedToFetch != 0)
                {
                    MessageBox.Show("Failed to check for updates", $"Failed to check for updates on {failedToFetch} mods, please check the logs for more info.");
                }
            }
            finally
            {
                isBusy = false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return CheckForUpdateResult.Canceled;
            }

            return availableUpdates == 0 ? CheckForUpdateResult.UpToDate : CheckForUpdateResult.UpdateAvailable;
        }

        public struct FetchResult
        {
            public int UpdatesFound;
            public int FailedToFetch;
        }

        private FetchResult CheckForUpdatesTaskVoid()
        {
            int updatesFound = 0;
            int failed = 0;
            while (queue.TryDequeue(out var mod) && !cancellationToken.IsCancellationRequested)
            {
                CheckForUpdateResult result = CheckForUpdateResult.UpToDate;
                switch (mod.Kind)
                {
                    case ModKind.Local:
                        if ((mod.Flags & ModFlags.Git) != 0)
                        {
                            result = CheckGitUpdate(mod);
                        }
                        break;

                    case ModKind.Steam:
                        break;
                }

                if (result == CheckForUpdateResult.UpdateAvailable)
                {
                    mod.Flags |= ModFlags.UpdateAvailable;
                    ++updatesFound;
                }
                else if (result == CheckForUpdateResult.Failure)
                {
                    ++failed;
                }
                Interlocked.Increment(ref processed);
            }

            return new FetchResult() { UpdatesFound = updatesFound, FailedToFetch = failed };
        }

        private static CheckForUpdateResult CheckGitUpdate(RimMod mod)
        {
            try
            {
                using Repository repository = new(mod.Path);
                var remote = repository.Network.Remotes["origin"];
                if (remote == null) return CheckForUpdateResult.UpToDate;
                FetchOptions fetchOptions = new();
                repository.Network.Fetch(remote.Name, remote.FetchRefSpecs.Select(x => x.Specification), fetchOptions);

                var localBranch = repository.Head;
                Branch? remoteBranch = null;
                if (localBranch.UpstreamBranchCanonicalName != null)
                {
                    remoteBranch = repository.Branches[localBranch.UpstreamBranchCanonicalName];
                }
                else
                {
                    var refs = repository.Network.ListReferences(remote);
                    var defaultBranchRef = refs.Select(x => x as SymbolicReference).FirstOrDefault(r => r != null && r.CanonicalName == "HEAD");
                    if (defaultBranchRef != null)
                    {
                        var defaultBranchName = $"refs/remotes/origin/{Path.GetFileName(defaultBranchRef.TargetIdentifier.AsSpan())}";

                        remoteBranch = repository.Branches[defaultBranchName];
                    }
                }
                if (remoteBranch == null) return CheckForUpdateResult.UpToDate;

                var aheadBehind = repository.ObjectDatabase.CalculateHistoryDivergence(localBranch.Tip, remoteBranch.Tip);
                return aheadBehind.BehindBy > 0 ? CheckForUpdateResult.UpdateAvailable : CheckForUpdateResult.UpToDate;
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                return CheckForUpdateResult.Failure;
            }
        }

        public async Task<bool> UpdateModsAsync(RimModList mods, IProgress<float>? progress = null, CancellationToken token = default)
        {
            if (isBusy) return false;
            if (availableUpdates == -1)
            {
                if (await CheckForUpdatesAsync(mods, progress, token) != CheckForUpdateResult.UpdateAvailable)
                {
                    return false;
                }
            }

            if (availableUpdates <= 0) return false;
            isBusy = true;

            try
            {
                cancellationToken = token;
                foreach (var mod in mods)
                {
                    if ((mod.Flags & ModFlags.UpdateAvailable) != 0)
                    {
                        queue.Enqueue(mod);
                    }
                }

                processed = 0;
                totalTasks = queue.Count;
                progress?.Report(0);

                Task[] tasks = new Task[MaxConcurrentTasks];

                for (int i = 0; i < MaxConcurrentTasks; ++i)
                {
                    tasks[i] = Task.Run(UpdateModsTaskVoid, token);
                }

                Task? updateTicker = null;
                if (progress != null)
                {
                    updateTicker = Task.Run(async () =>
                    {
                        int p;
                        do
                        {
                            p = Volatile.Read(ref processed);
                            progress.Report(p / (float)totalTasks);
                            await Task.Delay(100);
                        } while (p != totalTasks && !token.IsCancellationRequested);
                    }, token);
                }

                await Task.WhenAll(tasks);

                if (updateTicker != null)
                    await updateTicker;
            }
            finally
            {
                isBusy = false;
            }

            return !cancellationToken.IsCancellationRequested;
        }

        private void UpdateModsTaskVoid()
        {
            while (queue.TryDequeue(out var mod) && !cancellationToken.IsCancellationRequested)
            {
                bool updatedSuccessfully = false;
                switch (mod.Kind)
                {
                    case ModKind.Local:
                        if ((mod.Flags & ModFlags.Git) != 0)
                        {
                            updatedSuccessfully = UpdateGitMod(mod);
                        }
                        break;

                    case ModKind.Steam:
                        break;
                }

                if (updatedSuccessfully)
                {
                    mod.Flags &= ~ModFlags.UpdateAvailable;
                    Interlocked.Decrement(ref availableUpdates);
                }
                else
                {
                    MessageBox.Show("Failed to update mod", $"Failed to update '{mod.Name}', please check the logs for more info.");
                }

                Interlocked.Increment(ref processed);
            }
        }

        private static Signature CreateGitSignature() => new("RimModManager", "rimmodmanager@example.com", DateTimeOffset.Now);

        private static bool UpdateGitMod(RimMod mod)
        {
            try
            {
                using Repository repository = new(mod.Path);

                var localBranch = repository.Head;
                var remoteBranch = repository.Branches["refs/remotes/origin/HEAD"];
                if (remoteBranch == null) return false;

                Stash? stash = null;
                if (repository.RetrieveStatus().IsDirty)
                {
                    stash = repository.Stashes.Add(CreateGitSignature(), "Temporary stash before updating mod");
                }

                try
                {
                    repository.Reset(ResetMode.Hard, remoteBranch.Tip);
                }
                catch (Exception ex)
                {
                    logger.Log(ex);
                    return false;
                }
                finally
                {
                    if (stash != null)
                    {
                        repository.Stashes.Apply(0);
                        repository.Stashes.Remove(0);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                return false;
            }

            return true;
        }
    }
}