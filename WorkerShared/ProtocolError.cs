namespace WorkerShared
{
    using System;
    using System.Buffers.Binary;
    using System.Text;

    public struct ProtocolError : IRecord
    {
        public ProtocolErrorType ErrorType;
        public string? ErrorMessage;

        public ProtocolError(ProtocolErrorType errorType, string? errorMessage)
        {
            ErrorType = errorType;
            ErrorMessage = errorMessage;
        }

        public readonly MessageType Type => MessageType.ProtocolError;

        public readonly int Length => 8 + ErrorMessageByteCount;

        private readonly int ErrorMessageByteCount => ErrorMessage != null ? Encoding.UTF8.GetByteCount(ErrorMessage) : 0;

        public int Read(ReadOnlySpan<byte> buffer)
        {
            ErrorType = (ProtocolErrorType)BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            int len = BinaryPrimitives.ReadInt32LittleEndian(buffer[4..]);
            if (len > 0)
            {
                ErrorMessage = Encoding.UTF8.GetString(buffer.Slice(8, len));
            }

            return 8 + len;
        }

        public readonly int Write(Span<byte> buffer)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, (uint)ErrorType);
            var len = ErrorMessageByteCount;
            BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], len);
            Encoding.UTF8.GetBytes(ErrorMessage, buffer.Slice(8, len));
            return 8 + len;
        }
    }
}
