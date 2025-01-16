using System.Buffers.Binary;

namespace WorkerShared
{
    public struct IPCMessage
    {
        public MessageType Type;
        public uint Length;
        public Memory<byte> Data;

        public const int HeaderSize = 6;

        public IPCMessage(MessageType type, uint length)
        {
            Type = type;
            Length = length;
        }

        public readonly void Write(Span<byte> data)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(data, (ushort)Type);
            BinaryPrimitives.WriteUInt32LittleEndian(data[2..], Length);
        }

        public void Read(ReadOnlySpan<byte> data)
        {
            Type = (MessageType)BinaryPrimitives.ReadUInt16LittleEndian(data);
            Length = BinaryPrimitives.ReadUInt32LittleEndian(data[2..]);
        }

        public readonly T ReadDataAs<T>() where T : IRecord, new()
        {
            T t = new();
            t.Read(Data.Span);
            return t;
        }
    }
}