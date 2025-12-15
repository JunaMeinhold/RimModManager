using System.Buffers.Binary;

namespace WorkerShared
{
    public struct Heartbeat : IRecord
    {
        public long Timestamp;
        public WorkerState StateFlags;

        public Heartbeat(long timestamp, WorkerState flags)
        {
            Timestamp = timestamp;
            StateFlags = flags;
        }

        public readonly MessageType Type => MessageType.Heartbeat;

        public readonly int Length => 12;

        public int Read(ReadOnlySpan<byte> buffer)
        {
            Timestamp = BinaryPrimitives.ReadInt64LittleEndian(buffer);
            StateFlags = (WorkerState)BinaryPrimitives.ReadUInt32LittleEndian(buffer[8..]);
            return 12;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer, Timestamp);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], (uint)StateFlags);
            return 12;
        }
    }
}