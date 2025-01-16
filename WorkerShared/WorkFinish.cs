using System.Buffers.Binary;
using System.Text;

namespace WorkerShared
{
    public struct JobFinish : IRecord
    {
        public int Id;
        public int ResultCode;
        public string? Message;

        public JobFinish(int id, int resultCode, string? message = null)
        {
            Id = id;
            ResultCode = resultCode;
            Message = message;
        }

        public readonly MessageType Type => MessageType.JobFinish;

        public readonly int Length
        {
            get
            {
                return 4 + 4 + 4 + Encoding.UTF8.GetByteCount(Message ?? string.Empty);
            }
        }

        public int Read(ReadOnlySpan<byte> buffer)
        {
            Id = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            ResultCode = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]);
            int len = BinaryPrimitives.ReadInt32LittleEndian(buffer[8..]);
            Message = Encoding.UTF8.GetString(buffer.Slice(12, len));
            return 12 + len;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, Id);
            BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], ResultCode);
            int len = Encoding.UTF8.GetByteCount(Message ?? string.Empty);
            BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], len);
            if (Message != null)
            {
                Encoding.UTF8.GetBytes(Message, buffer[12..]);
            }
            return 12 + len;
        }
    }
}