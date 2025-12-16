namespace WorkerShared
{
    using System;
    using System.Buffers.Binary;

    public struct WorkerStateChanged : IRecord
    {
        public WorkerState State;

        public WorkerStateChanged(WorkerState state)
        {
            State = state;
        }

        public readonly MessageType Type => MessageType.WorkerStateChanged;

        public readonly int Length => 4;

        public int Read(ReadOnlySpan<byte> buffer)
        {
            State = (WorkerState)BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            return 4;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)State);
            return 4;
        }
    }
}
