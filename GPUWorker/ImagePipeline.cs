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
        private readonly SemaphoreSlim workerSignal = new(0, 1);

        private bool disposedValue;
        private bool outOfWork = true;

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
        }

        private Task OutOfWorkHandler(WorkerIPCClient client, IPCMessage message)
        {
            outOfWork = true;
            return Task.CompletedTask;
        }

        private Task JobPayloadBatchHandler(WorkerIPCClient client, IPCMessage message)
        {
            JobPayloadBatch batch = message.ReadDataAs<JobPayloadBatch>();
            EnqueueBatch(batch.Jobs);
            return Task.CompletedTask;
        }

        private Task JobPayloadHandler(WorkerIPCClient client, IPCMessage message)
        {
            JobPayload workload = message.ReadDataAs<JobPayload>();
            Enqueue(workload);
            return Task.CompletedTask;
        }

        private Task JobReadyHandler(WorkerIPCClient client, IPCMessage message)
        {
            outOfWork = false;
            SignalWorker();
            return Task.CompletedTask;
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
        }

        protected virtual async ValueTask RequestJob(CancellationToken cancellationToken)
        {
            if (!outOfWork)
            {
                if (batched)
                {
                    JobRequestBatch requestBatch = new(batchSize);
                    await client.SendMessageAsync(requestBatch, cancellationToken);
                }
                else
                {
                    await client.SendMessageRawAsync(new IPCMessage(MessageType.JobRequest, 0), cancellationToken);
                }
            }
        }

        public void Enqueue(JobPayload payload)
        {
            queue.Enqueue(payload);
            SignalWorker();
        }

        public void EnqueueBatch(IEnumerable<JobPayload> batch)
        {
            foreach (var payload in batch)
            {
                queue.Enqueue(payload);
            }

            SignalWorker();
        }

        private void SignalWorker()
        {
            if (workerSignal.CurrentCount == 0)
            {
                workerSignal.Release();
            }
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