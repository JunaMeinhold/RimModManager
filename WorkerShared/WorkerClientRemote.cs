namespace WorkerShared
{
    using System;
    using System.Buffers;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class WorkerClientRemote : IDisposable
    {
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private readonly Dictionary<MessageType, Func<WorkerClientRemote, IPCMessage, Task>> handlers = [];
        private Task? messageHandlerTask;
        private bool clientReady;
        private readonly SemaphoreSlim clientReadyHandle = new(0);
        private Task? heartbeatTask;
        private bool disposedValue;

        private long lastReceivedHeartbeat;
        private TimeSpan latency;

        private TimeSpan timeout = TimeSpan.FromSeconds(1);

        public WorkerClientRemote(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();

            SetHandler(MessageType.Heartbeat, HeartbeatHandler);
            SetHandler(MessageType.ClientReady, ClientReadyHandler);
        }

        public TimeSpan Latency => latency;

        public TimeSpan Timeout { get => timeout; set => timeout = value; }

        public event Action<WorkerClientRemote, bool>? Disconnected;

        public event Func<WorkerClientRemote, IPCMessage, Task>? MessageReceived;

        public event Func<WorkerClientRemote, Task>? Ready;

        public object? Tag { get; set; }

        public bool ClientReady => clientReady;

        public void SetHandler(MessageType type, Func<WorkerClientRemote, IPCMessage, Task> handler)
        {
            handlers[type] = handler;
        }

        public async Task ReadyClient()
        {
            messageHandlerTask = Task.Factory.StartNew(async () => await HandleMessagesAsync(cancellationTokenSource.Token), TaskCreationOptions.LongRunning);
            await SendMessageRawAsync(new(MessageType.ServerReady, 0));
        }

        private Task HeartbeatHandler(WorkerClientRemote remote, IPCMessage message)
        {
            Heartbeat heartbeat = message.ReadDataAs<Heartbeat>();
            long now = DateTime.UtcNow.Ticks;
            latency = TimeSpan.FromTicks(now - heartbeat.Timestamp);
            lastReceivedHeartbeat = now;
            return Task.CompletedTask;
        }

        private async Task ClientReadyHandler(WorkerClientRemote remote, IPCMessage message)
        {
            clientReady = true;
            clientReadyHandle.Release();
            heartbeatTask = Task.Factory.StartNew(async () => await HandleHeartbeat(cancellationTokenSource.Token));

            if (Ready != null)
            {
                await Ready.Invoke(remote);
            }
        }

        private async Task HandleHeartbeat(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Heartbeat heartbeat = new(DateTime.UtcNow.Ticks);
                await SendMessageAsync(heartbeat, cancellationToken);
                await Task.Delay(timeout, cancellationToken);

                if (lastReceivedHeartbeat < heartbeat.Timestamp)
                {
                    Terminate();
                }
            }
        }

        public void Terminate()
        {
            Dispose();
            Disconnected?.Invoke(this, true);
        }

        public async void Shutdown()
        {
            await SendMessageRawAsync(new IPCMessage(MessageType.Shutdown, 0));
            Dispose();
            Disconnected?.Invoke(this, false);
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
                    else
                    {
                        if (MessageReceived != null)
                        {
                            await MessageReceived.Invoke(this, message);
                        }
                    }
                }
                catch (Exception ex)
                {
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

            return message;
        }

        public async Task SendMessageAsync<T>(T record, CancellationToken cancellationToken = default) where T : IRecord
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

        public async Task SendMessageRawAsync(IPCMessage message, CancellationToken cancellationToken = default)
        {
            await clientReadyHandle.WaitAsync(cancellationToken);
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
                clientReadyHandle.Release();
            }
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                cancellationTokenSource.Cancel();
                clientReadyHandle.Dispose();
                heartbeatTask?.Wait();
                messageHandlerTask?.Wait();
                stream.Dispose();
                client.Dispose();
                disposedValue = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}