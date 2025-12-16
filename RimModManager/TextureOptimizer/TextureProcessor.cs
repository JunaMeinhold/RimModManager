namespace RimModManager.TextureOptimizer
{
    using Hexa.NET.D3D11;
    using Hexa.NET.DXGI;
    using Hexa.NET.Logging;
    using Hexa.NET.Utilities.IO;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using WorkerShared;

    public class TextureProcessor
    {
        private readonly ConcurrentQueue<JobPayload> queue = new();
        private readonly int port = 22984;
        private WorkerServer? server;
        private long total;
        private long processed;
        private bool isRunning;
        private bool isProcessing;
        private Task? scanTask;
        private Process[]? processes;
        private int idleWorkerCount = 0;

        public class WorkerStateObj
        {
            private long state;

            public bool Idle { get; set; } = true;

            public WorkerState State => (WorkerState)Volatile.Read(ref state);

            public WorkerState Exchange(WorkerState newState)
            {
                return (WorkerState)Interlocked.Exchange(ref state, (long)newState);
            }
        }

        public TextureProcessor()
        {
        }

        public long Total => total;

        public long Processed => processed;

        public bool IsProcessing => isProcessing;

        public bool OverwriteFiles { get; set; }

        public bool GenerateMips { get; set; }

        public bool UpscaleTextures { get; set; }

        public bool DownscaleTextures { get; set; }

        public int MinSize { get; set; } = 256;

        public int MaxSize { get; set; } = 1024;

        public bool BC7Quick { get; set; } = true;

        public bool DryRun { get; set; } = false;

        public event Action<LogMessage>? LogMessage;

        public WorkerServer? Server => server;

        public void StartWorkers(int workerCount = 8)
        {
            if (isRunning) return;
            isRunning = true;
            server = new(port);
            _ = server.StartAsync();
            server.Ready += ClientReady;
            server.StateChanged += OnStateChanged;
            server.SetHandler(MessageType.JobRequest, JobRequestHandler);
            server.SetHandler(MessageType.JobFinish, JobFinishHandler);
            server.SetHandler(MessageType.JobRequestBatch, JobRequestBatchHandler);
            server.SetHandler(MessageType.JobFinishBatch, JobFinishBatchHandler);

            processes = new Process[workerCount];

            for (int i = 0; i < workerCount; i++)
            {
                ProcessStartInfo psi = new("GPUWorker.exe", ["localhost", port.ToString()])
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                Process process = Process.Start(psi)!;
                processes[i] = process;
            }

            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }

        private async Task OnStateChanged(WorkerClientRemote remote, WorkerState state)
        {
            if (!Enum.IsDefined(state))
            {
                OnLogMessage(LogSeverity.Error, "WorkerStateChanged", $"Worker reported invalid state: {state}");
                return;
            }

            WorkerStateObj stateObj = (WorkerStateObj)remote.Tag!;
            var oldState = stateObj.Exchange(state);
            if (state == WorkerState.Idle)
            {
                var count = Interlocked.Increment(ref idleWorkerCount);
                if (count == processes!.Length && !queue.IsEmpty)
                {
                    SignalWorkers();
                }
            }
            else if (oldState == WorkerState.Idle)
            {
                Interlocked.Decrement(ref idleWorkerCount);
            }
        }

        private Task ClientReady(WorkerClientRemote remote)
        {
            remote.Tag = new WorkerStateObj();
            return Task.CompletedTask;
        }

        private Task JobFinishBatchHandler(WorkerClientRemote remote, IPCMessage message)
        {
            WorkerStateObj state = (WorkerStateObj)remote.Tag!;
            state.Idle = true;
            var batch = message.ReadDataAs<JobFinishBatch>();
            foreach (var job in batch.JobFinishes)
            {
                LogMessage?.Invoke(new LogMessage(null!, job.ResultCode == 0 ? LogSeverity.Info : LogSeverity.Error, job.Id.ToString(), job.Message!));
            }

            var value = Interlocked.Add(ref processed, batch.BatchSize);

            if (value == total && scanTask != null && scanTask.IsCompleted)
            {
                isProcessing = false;
            }
            return Task.CompletedTask;
        }

        private async Task JobRequestBatchHandler(WorkerClientRemote remote, IPCMessage message)
        {
            WorkerStateObj state = (WorkerStateObj)remote.Tag!;
            if (!state.Idle)
            {
                OnLogMessage(LogSeverity.Warning, "JobRequestBatchHandler", "Worker requested batch while not idle.");
                return;
            }

            state.Idle = false;

            JobRequestBatch requestBatch = message.ReadDataAs<JobRequestBatch>();

            int maxBatchSize = requestBatch.MaxBatchSize;

            JobPayload[] jobs = ArrayPool<JobPayload>.Shared.Rent(maxBatchSize);
            try
            {
                int batch = 0;
                while (batch < maxBatchSize && queue.TryDequeue(out var job))
                {
                    jobs[batch++] = job;
                }

                if (batch == 0)
                {
                    state.Idle = true;
                    await remote.SendLightMessageAsync(MessageType.OutOfWork);
                }
                else
                {
                    JobPayloadBatch payloadBatch = new(batch, jobs);
                    await remote.SendMessageAsync(payloadBatch);
                }
            }
            finally
            {
                ArrayPool<JobPayload>.Shared.Return(jobs);
            }
        }

        public void StopWorkers()
        {
            if (!isRunning) return;
            AppDomain.CurrentDomain.ProcessExit -= ProcessExit;

            server!.Stop();
            foreach (var process in processes!)
            {
                process.Kill();
            }
        }

        private void ProcessExit(object? sender, EventArgs e)
        {
            if (!isRunning) return;
            server!.Stop();
            foreach (var process in processes!)
            {
                process.Kill();
            }
        }

        public void ProcessPath(string path)
        {
            if (isProcessing) return;
            isProcessing = true;

            scanTask = Task.Run(() => ScanFolder(path));
        }

        public void ProcessPaths(params string[] folders)
        {
            if (isProcessing) return;
            isProcessing = true;

            scanTask = Task.Run(() =>
            {
                total = 0;
                processed = 0;
                queue.Clear();
                foreach (var folder in folders)
                {
                    ScanFolder(folder, false);
                }
            });
        }

        private void ScanFolder(string path, bool clearState = true)
        {
            if (clearState)
            {
                total = 0;
                processed = 0;
                queue.Clear();
            }

            int workerCount = processes!.Length;
            int batch = 0;
            foreach (var metadata in FileUtils.EnumerateEntries(path!, "", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(metadata.Path);
                if (!extension.SequenceEqual(".png") && !extension.SequenceEqual(".jpg") && !extension.SequenceEqual(".jpeg"))
                {
                    continue;
                }

                var file = metadata.Path.ToString();
                var folder = Path.GetDirectoryName(file)!;
                var fileNameNoExt = Path.GetFileNameWithoutExtension(file);
                var target = Path.Combine(folder, fileNameNoExt + ".dds");

                if (File.Exists(target))
                {
                    if (!OverwriteFiles)
                    {
                        OnLogMessage(LogSeverity.Warning, file, "Target dds file already exists, skipped.");
                        continue;
                    }

                    File.Delete(target);
                }

                WorkloadFlags flags = WorkloadFlags.FlipVertical;
                if (GenerateMips) flags |= WorkloadFlags.GenerateMips;
                if (UpscaleTextures) flags |= WorkloadFlags.Upscale;
                if (DownscaleTextures) flags |= WorkloadFlags.Downscale;
                if (BC7Quick) flags |= WorkloadFlags.Bc7Quick;
                if (DryRun) flags |= WorkloadFlags.DryRun;

                Format format = Format.Bc7Unorm;

                if (fileNameNoExt.EndsWith('m') && File.Exists(Path.Combine(folder, fileNameNoExt[..^1] + Path.GetExtension(path))))
                {
                    format = Format.Bc1Unorm;
                }

                JobPayload workload = new(total, file, target, flags, (int)format, MinSize, MaxSize);

                queue.Enqueue(workload);
                total++;
                batch++;
                if (batch >= workerCount)
                {
                    SignalWorkers();
                    batch = 0;
                }
            }

            if (batch > 0)
            {
                SignalWorkers();
            }
        }

        public void Cancel()
        {
            if (!isProcessing) return;
            queue.Clear();
            isProcessing = false;
        }

        private void SignalWorkers()
        {
            _ = server!.BroadcastLightMessageAsync(MessageType.JobReady);
        }

        private Task JobFinishHandler(WorkerClientRemote remote, IPCMessage message)
        {
            WorkerStateObj state = (WorkerStateObj)remote.Tag!;
            if (state.Idle)
            {
                OnLogMessage(LogSeverity.Warning, "JobFinishHandler", "Worker reported job finish while idle.");
                return Task.CompletedTask;
            }
            state.Idle = true;
            var finish = message.ReadDataAs<JobFinish>();
            var value = Interlocked.Increment(ref processed);
            LogMessage?.Invoke(new LogMessage(null!, finish.ResultCode == 0 ? LogSeverity.Info : LogSeverity.Error, finish.Id.ToString(), finish.Message!));
            if (value == total && scanTask != null && scanTask.IsCompleted)
            {
                isProcessing = false;
            }
            return Task.CompletedTask;
        }

        private async Task JobRequestHandler(WorkerClientRemote remote, IPCMessage message)
        {
            WorkerStateObj state = (WorkerStateObj)remote.Tag!;
            if (!state.Idle)
            {
                OnLogMessage(LogSeverity.Warning, "JobRequestHandler", "Worker requested job while not idle.");
                return;
            }
            state.Idle = false;

            if (queue.TryDequeue(out var item))
            {
                await remote.SendMessageAsync(item);
            }
            else
            {
                await remote.SendLightMessageAsync(MessageType.OutOfWork);
                state.Idle = true;
            }
        }

        public void OnLogMessage(LogSeverity severity, string source, string message)
        {
            LogMessage msg = new(null!, severity, source, message);
            LogMessage?.Invoke(msg);
        }
    }
}