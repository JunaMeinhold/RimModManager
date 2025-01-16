using System.Buffers.Binary;

namespace WorkerShared
{
    public struct Heartbeat : IRecord
    {
        public long Timestamp;

        public Heartbeat(long timestamp)
        {
            Timestamp = timestamp;
        }

        public readonly MessageType Type => MessageType.Heartbeat;

        public readonly int Length => 8;

        public int Read(ReadOnlySpan<byte> buffer)
        {
            Timestamp = BinaryPrimitives.ReadInt64LittleEndian(buffer);
            return 8;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer, Timestamp);
            return 8;
        }
    }
}