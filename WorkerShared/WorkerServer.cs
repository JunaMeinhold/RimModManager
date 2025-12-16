namespace WorkerShared
{
    using RimModManager;
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

        public event Func<WorkerClientRemote, WorkerState, Task>? StateChanged;

        public event Func<WorkerClientRemote, Heartbeat, Task>? HeartbeatReceived;

        public event Func<WorkerClientRemote, Task>? HeartbeatMissed;

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
            SubscribeEvents(remote);

            await remote.HandshakeAsync();

            await semaphore.WaitAsync();
            clients.Add(remote);
            semaphore.Release();
            Connected?.Invoke(remote);
        }



        private async Task OnHeartbeatReceived(WorkerClientRemote remote, Heartbeat heartbeat)
        {
            if (HeartbeatReceived != null)
            {
                await HeartbeatReceived.Invoke(remote, heartbeat);
            }
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
            UnsubscribeEvents(remote);
            semaphore.Wait();
            clients.Remove(remote);
            semaphore.Release();
            Disconnected?.Invoke(remote, terminated);
        }
        private void SubscribeEvents(WorkerClientRemote remote)
        {
            remote.Disconnected += OnClientDisconnected;
            remote.MessageReceived += OnMessageReceived;
            remote.StateChanged += OnStateChanged;
            remote.HeartbeatReceived += OnHeartbeatReceived;
            remote.HeartbeatMissed += OnHeartbeatMissed;
            remote.Ready += OnClientReady;
        }

        private void UnsubscribeEvents(WorkerClientRemote remote)
        {
            remote.HeartbeatReceived -= OnHeartbeatReceived;
            remote.HeartbeatMissed -= OnHeartbeatMissed;
            remote.StateChanged -= OnStateChanged;
            remote.MessageReceived -= OnMessageReceived;
            remote.Disconnected -= OnClientDisconnected;
            remote.Ready -= OnClientReady;
        }

        private async Task OnStateChanged(WorkerClientRemote remote, WorkerState state)
        {
            if (StateChanged != null)
            {
                await StateChanged.Invoke(remote, state);
            }
        }

        private async Task OnHeartbeatMissed(WorkerClientRemote remote)
        {
            if (HeartbeatMissed != null)
            {
                await HeartbeatMissed.Invoke(remote);
            }
        }

        private async Task OnMessageReceived(WorkerClientRemote remote, IPCMessage message)
        {
            if (handlers.TryGetValue(message.Type, out var handler))
            {
                await handler(remote, message);
            }
        }

        public Task BroadcastLightMessageAsync(MessageType message, CancellationToken cancellationToken = default)
        {
            return BroadcastMessageAsync(new LightIPCMessage(message), cancellationToken);
        }

        public async Task BroadcastMessageAsync<T>(T record, CancellationToken cancellationToken = default) where T : IRecord
        {
            using var guard = await semaphore.LockAsync(cancellationToken);
            var length = record.Length;
            var totalLength = IPCMessage.HeaderSize + length;
            var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
            try
            {
                var span = buffer.AsSpan(0, totalLength);
                IPCMessage message = new(record.Type, (uint)length);
                message.Write(span);
                record.Write(span[IPCMessage.HeaderSize..]);

                foreach (var remote in clients)
                {
                    await remote.SendRawAsync(buffer.AsMemory(0, totalLength), cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            semaphore.Wait();
            foreach (var remote in clients)
            {
                UnsubscribeEvents(remote);
                remote.Shutdown();
            }
            semaphore.Dispose();

            isRunning = false;
            listener.Stop();
        }

        public struct ClientEnumerator : IEnumerator<WorkerClientRemote>
        {
            private List<WorkerClientRemote> list;
            private SemaphoreSlim semaphore;
            private int index;

            public ClientEnumerator(List<WorkerClientRemote> list, SemaphoreSlim semaphore)
            {
                this.list = list;
                this.semaphore = semaphore;
                this.index = -1;
                semaphore.Wait();
            }

            public WorkerClientRemote Current => list[index];

            object? System.Collections.IEnumerator.Current => Current;

            public void Dispose()
            {
                semaphore.Release();
            }

            public bool MoveNext()
            {
                index++;
                return index < list.Count;
            }
            public void Reset()
            {
                index = -1;

            }

        }

        public ClientEnumerator GetEnumerator()
        {
            return new ClientEnumerator(clients, semaphore);
        }
    }
}