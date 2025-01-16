namespace WorkerShared
{
    using Hexa.NET.Protobuf;

    [ProtobufRecord]
    public partial struct JobPayload : IRecord
    {
        public int Id;
        public string Source;
        public string Destination;
        public WorkloadFlags Flags;
        public int Format;
        public int MinSize;
        public int MaxSize;

        public JobPayload(int id, string source, string destination, WorkloadFlags flags, int format, int minSize, int maxSize)
        {
            Id = id;
            Source = source;
            Destination = destination;
            Flags = flags;
            Format = format;
            MinSize = minSize;
            MaxSize = maxSize;
        }

        public readonly MessageType Type => MessageType.JobPayload;

        public readonly int Length
        {
            get
            {
                return SizeOf();
            }
        }
    }
}