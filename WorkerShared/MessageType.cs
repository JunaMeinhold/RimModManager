namespace WorkerShared
{
    using System;
    using System.Buffers.Binary;

    public enum MessageType : ushort
    {
        Unknown = 0,
        ClientReady,
        ServerReady,
        JobReady,
        JobRequest,
        JobPayload,
        JobFinish,

        OutOfWork,

        JobRequestBatch,
        JobPayloadBatch,
        JobFinishBatch,

        Heartbeat,
        Shutdown,
    }

    public struct JobRequestBatch : IRecord
    {
        public int MaxBatchSize;

        public JobRequestBatch(int maxBatchSize)
        {
            MaxBatchSize = maxBatchSize;
        }

        public readonly MessageType Type => MessageType.JobRequestBatch;

        public readonly int Length => 4;

        public int Read(ReadOnlySpan<byte> buffer)
        {
            MaxBatchSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            return 4;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, MaxBatchSize);
            return 4;
        }
    }

    public struct JobPayloadBatch : IRecord
    {
        public int BatchSize;
        public JobPayload[] Jobs;

        public JobPayloadBatch(int batchSize, JobPayload[] jobs)
        {
            BatchSize = batchSize;
            Jobs = jobs;
        }

        public readonly MessageType Type => MessageType.JobPayloadBatch;

        public readonly int Length
        {
            get
            {
                int size = 4;
                for (int i = 0; i < BatchSize; i++)
                {
                    size += Jobs[i].Length;
                }
                return size;
            }
        }

        public int Read(ReadOnlySpan<byte> buffer)
        {
            BatchSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            Jobs = new JobPayload[BatchSize];
            int idx = 4;
            for (int i = 0; i < BatchSize; i++)
            {
                idx += Jobs[i].Read(buffer[idx..]);
            }
            return idx;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, BatchSize);
            int idx = 4;
            for (int i = 0; i < BatchSize; i++)
            {
                idx += Jobs[i].Write(buffer[idx..]);
            }
            return idx;
        }
    }

    public struct JobFinishBatch : IRecord
    {
        public int BatchSize;
        public JobFinish[] JobFinishes;

        public JobFinishBatch(int batchSize, JobFinish[] jobFinishes)
        {
            BatchSize = batchSize;
            JobFinishes = jobFinishes;
        }

        public readonly MessageType Type => MessageType.JobFinishBatch;

        public readonly int Length
        {
            get
            {
                return 4 + JobFinishes.Sum(x => x.Length);
            }
        }

        public int Read(ReadOnlySpan<byte> buffer)
        {
            BatchSize = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            JobFinishes = new JobFinish[BatchSize];
            int idx = 4;
            for (int i = 0; i < BatchSize; i++)
            {
                idx += JobFinishes[i].Read(buffer[idx..]);
            }
            return idx;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, BatchSize);
            int idx = 4;
            for (int i = 0; i < BatchSize; i++)
            {
                idx += JobFinishes[i].Write(buffer[idx..]);
            }
            return idx;
        }
    }
}