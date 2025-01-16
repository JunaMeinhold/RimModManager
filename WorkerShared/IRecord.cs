namespace WorkerShared
{
    public interface IRecord
    {
        public MessageType Type { get; }

        public int Length { get; }

        public int Read(ReadOnlySpan<byte> buffer);

        public int Write(Span<byte> buffer);
    }
}