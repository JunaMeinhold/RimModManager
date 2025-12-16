using System.Buffers.Binary;

namespace WorkerShared
{
    public struct Heartbeat : IRecord
    {
        public long Timestamp;
        public WorkerState State;

        public Heartbeat(long timestamp, WorkerState state)
        {
            Timestamp = timestamp;
            State = state;
        }

        public readonly MessageType Type => MessageType.Heartbeat;

        public readonly int Length => 12;

        public int Read(ReadOnlySpan<byte> buffer)
        {
            Timestamp = BinaryPrimitives.ReadInt64LittleEndian(buffer);
            State = (WorkerState)BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]);
            return 12;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer, Timestamp);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], (uint)State);
            return 12;
        }
    }
}