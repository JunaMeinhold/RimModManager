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
        private readonly SemaphoreSlim serverReadySignal = new(0);

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

        public WorkerState State { get; private set; }

        public async Task SetStateAsync(WorkerState newState)
        {
            State = newState;
            WorkerStateChanged stateChanged = new(newState);
            await SendMessageAsync(stateChanged);
        }

        public async Task HandshakeAsync()
        {
            client = new(masterAddress, masterPort);
            stream = client.GetStream();

            SetMessageHandler(MessageType.Heartbeat, HeartbeatHandler);
            SetMessageHandler(MessageType.Shutdown, ShutdownHandler);
            SetMessageHandler(MessageType.ServerReady, ServerReadyHandler);

            messageHandlerTask = Task.Factory.StartNew(async () => await HandleMessagesAsync(cancellationTokenSource.Token), cancellationTokenSource.Token);

            await SendLightMessageAsync(MessageType.ClientReady);
            await serverReadySignal.WaitAsync();
        }

        private Task ServerReadyHandler(WorkerIPCClient client, IPCMessage message)
        {
            serverReadySignal.Release();
            return Task.CompletedTask;
        }

        public async Task WaitForExit() 
        {
            await Task.Delay(-1, cancellationTokenSource.Token);
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
            Heartbeat heartbeat = new(now, State);
            await client.SendMessageAsync(heartbeat);
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
            return message;
        }

        public async ValueTask SendErrorAsync(ProtocolErrorType errorType, string? errorMessage = null)
        {
            ProtocolError error = new(errorType, errorMessage);
            await SendMessageAsync(error);
        }

        public async ValueTask SendLightMessageAsync(MessageType type, CancellationToken cancellationToken = default)
        {
            await SendMessageAsync(new LightIPCMessage(type), cancellationToken);
        }

        public async ValueTask SendMessageAsync<T>(T record, CancellationToken cancellationToken = default) where T : IRecord
        {
            var length = record.Length;
            var totalLength = IPCMessage.HeaderSize + length;
            var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
            try
            {
                var span = buffer.AsSpan();
                IPCMessage message = new(record.Type, (uint)length);
                message.Write(span);
                record.Write(span[IPCMessage.HeaderSize..]);
                await stream.WriteAsync(buffer.AsMemory(0, totalLength), cancellationToken);
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