namespace WorkerShared
{
    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class WorkerServer
    {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        private bool isRunning;
        private readonly List<WorkerClientRemote> clients = [];
        private readonly Dictionary<MessageType, Func<WorkerClientRemote, IPCMessage, Task>> handlers = [];

        private readonly SemaphoreSlim semaphore = new(1);

        public WorkerServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            isRunning = false;
        }

        public event Action<WorkerClientRemote>? Connected;

        public event Action<WorkerClientRemote, bool>? Disconnected;

        public event Func<WorkerClientRemote, Task>? Ready;

        public IReadOnlyList<WorkerClientRemote> Clients => clients;

        public void SetHandler(MessageType type, Func<WorkerClientRemote, IPCMessage, Task> handler)
        {
            handlers[type] = handler;
        }

        public async Task StartAsync()
        {
            listener.Start();
            isRunning = true;

            try
            {
                while (isRunning)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(cancellationTokenSource.Token);
                    _ = OnClientConnect(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                listener.Stop();
            }
        }

        private async Task OnClientConnect(TcpClient client)
        {
            WorkerClientRemote remote = new(client);
            remote.Disconnected += OnClientDisconnected;
            remote.MessageReceived += OnMessageReceived;
            remote.Ready += OnClientReady;

            await remote.ReadyClient();

            await semaphore.WaitAsync();
            clients.Add(remote);
            semaphore.Release();
            Connected?.Invoke(remote);
        }

        private async Task OnClientReady(WorkerClientRemote remote)
        {
            if (Ready != null)
            {
                await Ready.Invoke(remote);
            }
        }

        private void OnClientDisconnected(WorkerClientRemote remote, bool terminated)
        {
            remote.MessageReceived -= OnMessageReceived;
            remote.Disconnected -= OnClientDisconnected;
            remote.Ready -= OnClientReady;
            semaphore.Wait();
            clients.Remove(remote);
            semaphore.Release();
            Disconnected?.Invoke(remote, terminated);
        }

        private async Task OnMessageReceived(WorkerClientRemote remote, IPCMessage message)
        {
            if (handlers.TryGetValue(message.Type, out var handler))
            {
                await handler(remote, message);
            }
        }

        public async Task Broadcast<T>(T record, CancellationToken cancellationToken = default) where T : IRecord
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
                await BroadcastRaw(message, cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task BroadcastRaw(IPCMessage message, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                foreach (var remote in clients)
                {
                    await remote.SendMessageRawAsync(message, cancellationToken);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            semaphore.Wait();
            foreach (var remote in clients)
            {
                remote.MessageReceived -= OnMessageReceived;
                remote.Disconnected -= OnClientDisconnected;
                remote.Ready -= OnClientReady;
                remote.Shutdown();
            }
            semaphore.Dispose();

            isRunning = false;
            listener.Stop();
        }
    }
}