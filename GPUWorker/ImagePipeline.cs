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
        private Task? pipelineTask;
        private readonly SemaphoreSlim workerSignal = new(0, int.MaxValue);

        private bool disposedValue;
        private bool outOfWork = true;
        private WorkerState state = WorkerState.None;

        public ImagePipelineBase(WorkerIPCClient client, bool batched = true, int batchSize = 32)
        {
            this.client = client;
            this.batched = batched;
            this.batchSize = batchSize;
      
            client.SetMessageHandler(MessageType.JobReady, JobReadyHandler);
            client.SetMessageHandler(MessageType.JobPayload, JobPayloadHandler);
            client.SetMessageHandler(MessageType.OutOfWork, OutOfWorkHandler);
            client.SetMessageHandler(MessageType.JobPayloadBatch, JobPayloadBatchHandler);
            pipelineTask = PipelineTaskLoop(cancellationTokenSource.Token);
        }

        public async Task StartAsync()
        {
            state = WorkerState.Idle;
            await client.SetStateAsync(WorkerState.Idle);
        }

        private async Task<bool> TransitionState(WorkerState from, WorkerState to)
        {
            if (Interlocked.CompareExchange(ref state, to, from) == from)
            {
                await client.SetStateAsync(to);
                return true;
            }
            return false;
        }

        private async Task OutOfWorkHandler(WorkerIPCClient client, IPCMessage message)
        {
            await TransitionState(WorkerState.WaitingForJobRequest, WorkerState.Idle);
            outOfWork = true;
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
            await TransitionState(WorkerState.Busy, WorkerState.Idle);
        }

        protected virtual async ValueTask RequestJob(CancellationToken cancellationToken)
        {
            if (!await TransitionState(WorkerState.Idle, WorkerState.WaitingForJobRequest))
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

        private async Task Enqueue(JobPayload payload)
        {
            if (!await TransitionState(WorkerState.WaitingForJobRequest, WorkerState.Busy))
            {
                await client.SendErrorAsync(ProtocolErrorType.OutOfOrderMessage, "Cannot enqueue job payload when worker is not waiting for job request.");
                return;
            }

            queue.Enqueue(payload);
            SignalWorker();
        }

        private async Task EnqueueBatch(IEnumerable<JobPayload> batch)
        {
            if (!await TransitionState(WorkerState.WaitingForJobRequest, WorkerState.Busy))
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