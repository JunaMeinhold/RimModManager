namespace GPUWorker
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using WorkerShared;

    public abstract class ImagePipelineBase : IDisposable
    {
        private readonly WorkerIPCClient client;
        private readonly bool batched;
        private readonly int batchSize;
        private readonly ConcurrentQueue<JobPayload> queue = new();
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Task pipelineTask;
        private readonly SemaphoreSlim workerSignal = new(0, int.MaxValue);

        private bool disposedValue;
        private bool outOfWork = true;
        private WorkerState flags;

        public ImagePipelineBase(WorkerIPCClient client, bool batched = true, int batchSize = 32)
        {
            this.client = client;
            this.batched = batched;
            this.batchSize = batchSize;
            pipelineTask = Task.Factory.StartNew(async () => await PipelineTaskLoop(cancellationTokenSource.Token), TaskCreationOptions.LongRunning);
            client.SetMessageHandler(MessageType.JobReady, JobReadyHandler);
            client.SetMessageHandler(MessageType.JobPayload, JobPayloadHandler);
            client.SetMessageHandler(MessageType.OutOfWork, OutOfWorkHandler);
            if (batched)
            {
                client.SetMessageHandler(MessageType.JobPayloadBatch, JobPayloadBatchHandler);
            }
            client.StateFlags |= WorkerState.Idle;
            flags = WorkerState.Idle;
        }

        private bool TransitionState(WorkerState from, WorkerState to)
        {
            if (Interlocked.CompareExchange(ref flags, to, from) == from)
            {
                client.StateFlags = to;
                return true;
            }
            return false;
        }

        private Task OutOfWorkHandler(WorkerIPCClient client, IPCMessage message)
        {
            TransitionState(WorkerState.WaitingForJobRequest, WorkerState.Idle);
            outOfWork = true;
            return Task.CompletedTask;
        }

        private async Task JobPayloadBatchHandler(WorkerIPCClient client, IPCMessage message)
        {
            JobPayloadBatch batch = message.ReadDataAs<JobPayloadBatch>();
            await EnqueueBatch(batch.Jobs);
        }

        private async Task JobPayloadHandler(WorkerIPCClient client, IPCMessage message)
        {
            JobPayload workload = message.ReadDataAs<JobPayload>();
            await Enqueue(workload);
        }

        private async Task JobReadyHandler(WorkerIPCClient client, IPCMessage message)
        {
            outOfWork = false;
            await RequestJob(cancellationTokenSource.Token);
        }

        public async Task PipelineTaskLoop(CancellationToken token)
        {
            JobFinish[] jobs = new JobFinish[batchSize];
            while (!token.IsCancellationRequested)
            {
                await workerSignal.WaitAsync(token);

                if (!queue.IsEmpty)
                {
                    int i = 0;
                    while (queue.TryDequeue(out var workload))
                    {
                        jobs[i] = ProcessImage(workload, token);
                        i++;
                    }

                    await SendFinishJob(jobs, i, token);
                }

                await RequestJob(token);
            }
        }

        protected abstract JobFinish ProcessImage(JobPayload workload, CancellationToken cancellationToken);

        protected virtual async ValueTask SendFinishJob(JobFinish[] jobs, int batchSize, CancellationToken cancellationToken)
        {
            if (batched)
            {
                JobFinishBatch batch = new(batchSize, jobs);
                await client.SendMessageAsync(batch, cancellationToken);
            }
            else
            {
                JobFinish job = jobs[0];
                await client.SendMessageAsync(job, cancellationToken);
            }
            TransitionState(WorkerState.Busy, WorkerState.Idle);
        }

        protected virtual async ValueTask RequestJob(CancellationToken cancellationToken)
        {
            if (!TransitionState(WorkerState.Idle, WorkerState.WaitingForJobRequest))
            {
                return;
            }
            if (outOfWork)
            {
                return;
            }

            if (batched)
            {
                JobRequestBatch requestBatch = new(batchSize);
                await client.SendMessageAsync(requestBatch, cancellationToken);
            }
            else
            {
                await client.SendLightMessageAsync(MessageType.JobRequest, cancellationToken);
            }
        }

        public async Task Enqueue(JobPayload payload)
        {
            if (!TransitionState(WorkerState.WaitingForJobRequest, WorkerState.Busy))
            {
                await client.SendErrorAsync(ProtocolErrorType.OutOfOrderMessage, "Cannot enqueue job payload when worker is not waiting for job request.");
                return;
            }

            queue.Enqueue(payload);
            SignalWorker();
        }

        public async Task EnqueueBatch(IEnumerable<JobPayload> batch)
        {
            if (!TransitionState(WorkerState.WaitingForJobRequest, WorkerState.Busy))
            {
                await client.SendErrorAsync(ProtocolErrorType.OutOfOrderMessage, "Cannot enqueue job payload batch when worker is not waiting for job request.");
                return;
            }

            foreach (var payload in batch)
            {
                queue.Enqueue(payload);
            }

            SignalWorker();
        }

        private void SignalWorker()
        {
            workerSignal.Release();
        }

        protected virtual void DisposeCore()
        {
            if (!disposedValue)
            {
                cancellationTokenSource.Cancel();
                pipelineTask.Wait();
                workerSignal.Dispose();
                queue.Clear();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }
    }
}