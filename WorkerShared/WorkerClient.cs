using System.Buffers;
using System.Net.Sockets;

namespace WorkerShared
{
    public class WorkerIPCClient : IDisposable
    {
        private readonly string masterAddress;
        private readonly int masterPort;
        private TcpClient client;
        private NetworkStream stream;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Dictionary<MessageType, Func<WorkerIPCClient, IPCMessage, Task>> handlers = [];
        private Task messageHandlerTask;
        private bool disposedValue;

        private TimeSpan latency;

        public WorkerIPCClient(string masterAddress, int masterPort)
        {
            this.masterAddress = masterAddress;
            this.masterPort = masterPort;
            client = null!;
            stream = null!;
            messageHandlerTask = null!;
            cancellationTokenSource = new();
        }

        public TimeSpan Latency => latency;

        public async Task StartProcessingAsync()
        {
            client = new(masterAddress, masterPort);
            stream = client.GetStream();

            SetMessageHandler(MessageType.Heartbeat, HeartbeatHandler);
            SetMessageHandler(MessageType.Shutdown, ShutdownHandler);

            messageHandlerTask = Task.Factory.StartNew(async () => await HandleMessagesAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);

            await SendMessageRawAsync(new IPCMessage(MessageType.ClientReady, 0));

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                Thread.Sleep(1);
            }
        }

        private Task ShutdownHandler(WorkerIPCClient client, IPCMessage message)
        {
            cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        private async Task HeartbeatHandler(WorkerIPCClient client, IPCMessage message)
        {
            var received = message.ReadDataAs<Heartbeat>();
            long now = DateTime.UtcNow.Ticks;
            latency = TimeSpan.FromTicks(now - received.Timestamp);
            Heartbeat heartbeat = new(now);
            await client.SendMessageAsync<Heartbeat>(heartbeat);
            Console.WriteLine($"Heartbeat: Latency: {latency.Milliseconds}ms, {received.Timestamp}");
        }

        public void SetMessageHandler(MessageType type, Func<WorkerIPCClient, IPCMessage, Task> handler)
        {
            handlers[type] = handler;
        }

        private async Task HandleMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await ReceiveMessageAsync(cancellationToken);

                    if (handlers.TryGetValue(message.Type, out var handler))
                    {
                        await handler(this, message);
                    }
                }
                catch (Exception ex)
                {
                    if (!client.Connected)
                    {
                        Dispose();
                    }
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                }
            }
        }

        private readonly Memory<byte> messageHeaderBuffer = new byte[IPCMessage.HeaderSize];
        private Memory<byte> messageBuffer = new byte[1024];

        private async Task<IPCMessage> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            await stream.ReadExactlyAsync(messageHeaderBuffer, cancellationToken);
            IPCMessage message = default;
            message.Read(messageHeaderBuffer.Span);
            if (message.Length > messageBuffer.Length)
            {
                messageBuffer = new byte[message.Length];
            }
            message.Data = messageBuffer[..(int)message.Length];
            if (message.Length > 0)
            {
                await stream.ReadExactlyAsync(message.Data, cancellationToken);
            }
            Console.WriteLine($"Received message: {message.Type}, {message.Length}");
            return message;
        }

        public async ValueTask SendMessageAsync<T>(T record, CancellationToken cancellationToken = default) where T : IRecord
        {
            var length = record.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                record.Write(buffer);
                IPCMessage message = new(record.Type, (uint)length)
                {
                    Data = buffer.AsMemory()[..length]
                };
                await SendMessageRawAsync(message, cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async ValueTask SendMessageRawAsync(IPCMessage message, CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(IPCMessage.HeaderSize);
            try
            {
                Memory<byte> headerBuffer = buffer.AsMemory()[..IPCMessage.HeaderSize];
                message.Write(headerBuffer.Span);
                await stream.WriteAsync(headerBuffer, cancellationToken);
                await stream.WriteAsync(message.Data, cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                cancellationTokenSource.Cancel();
                messageHandlerTask.Wait();
                stream.Dispose();
                client.Dispose();
                disposedValue = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}