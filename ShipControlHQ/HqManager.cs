using ShipControlCommon;
using System.Net;
using System.Text;
using WebSocketSharp.Server;

namespace ShipControlHQ
{
    public sealed class HqManager
    {
        private static readonly Lazy<HqManager> instance =
            new Lazy<HqManager>(() => new HqManager());

        public static HqManager Instance { get { return instance.Value; } }

        public struct Status
        {
            public bool hqRunning;
            public List<Guid> connectedClientsId;
        }

        private readonly HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _listenerSemaphore;
        private bool _running;
        private readonly WebSocketServer _wsServer;

        private readonly Dictionary<Guid, ShipService> _services;
        private readonly SemaphoreSlim _servicesSemaphore;

        private HqManager()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8080/");
            _listenerSemaphore = new SemaphoreSlim(1, 1);
            _running = false;
            _wsServer = new WebSocketServer("ws://localhost:9000");
            _services = new Dictionary<Guid, ShipService>();
            _servicesSemaphore = new SemaphoreSlim(1, 1);
        }


        public async Task Start(string address)
        {
            await _listenerSemaphore.WaitAsync();
            try
            {
                if (_running)
                {
                    Console.WriteLine("Server is already running");
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    _listener.Start();
                    _wsServer.Start();
                    _running = true;

                    Console.WriteLine("Server started listening on " + address);

                    await Task.Factory.StartNew(async () =>
                    {
                        while (true)
                        {
                            var context = await _listener.GetContextAsync().ConfigureAwait(false);
                            Task.Run(() => ProcessRequestAsync(context, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                        }
                    }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            finally
            {
                _listenerSemaphore.Release();
            }
        }

        public async void Stop()
        {
            await _listenerSemaphore.WaitAsync();
            try
            {
                if (!_running)
                {
                    Console.WriteLine("Server is not running");
                    return;
                }

                _cancellationTokenSource.Cancel();
                _listener.Stop();
                _wsServer.Stop();
                _running = false;
                Console.WriteLine("Server is now shut down");
            }
            finally
            {
                _listenerSemaphore.Release();
            }
        }

        public Status GetStatus()
        {
            var status = new Status();

            _listenerSemaphore.Wait();
            try { status.hqRunning = _running; } finally { _listenerSemaphore.Release(); }

            _servicesSemaphore.Wait();
            try { status.connectedClientsId = new List<Guid>(_services.Keys); } finally { _servicesSemaphore.Release(); }

            return status;
        }

        public bool IsValidShipId(string shipIdStr)
        {
            if (string.IsNullOrEmpty(shipIdStr))
            {
                return false;
            }

            Guid clientId;
            if (!Guid.TryParse(shipIdStr, out clientId))
            {
                return false;
            }

            _servicesSemaphore.Wait();
            try
            {
                return _services.ContainsKey(clientId);
            }
            finally
            {
                _servicesSemaphore.Release();
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                var responseData = Encoding.UTF8.GetBytes("Begone, beast!");

                // Read client's id, AKA "verify" and "authenticate" the client :P
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    var newClientId = await reader.ReadToEndAsync().ConfigureAwait(false);
                    Guid clientId;
                    if (Guid.TryParse(newClientId, out clientId))
                    {
                        Console.WriteLine($"Accepting new client connection from '{newClientId}'");

                        await _servicesSemaphore.WaitAsync();
                        try
                        {
                            if (_services.TryGetValue(clientId, out var service))
                            {
                                responseData = Encoding.UTF8.GetBytes("Welcome back to HQ, you old parrot!");
                            }
                            else
                            {
                                responseData = Encoding.UTF8.GetBytes("Ahoy, new pirate!");
                                _services[clientId] = new ShipService();
                                _wsServer.AddWebSocketService<ShipService>($"/{clientId}", () => _services[clientId]);
                            }
                        }
                        finally
                        {
                            _servicesSemaphore.Release();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Ignoring connection attempt from unverified client");
                    }
                }

                response.ContentLength64 = responseData.Length;

                // Write response asynchronously
                await response.OutputStream.WriteAsync(responseData, 0, responseData.Length, cancellationToken).ConfigureAwait(false);
                response.OutputStream.Close();
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation exceptions
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex.Message}");
            }
        }

        public void DumpStatus()
        {
            Console.WriteLine("test");
        }

        public bool AddCommand(Guid shipId, Command cmd)
        {
            _servicesSemaphore.Wait();
            try
            {
                if (_services.ContainsKey(shipId))
                {
                    _services[shipId].SendCommand(cmd);
                    return true;
                }
                else
                {
                    Console.WriteLine($"Can't find ship with ID {shipId}");
                    return false;
                }
            }
            finally
            {
                _servicesSemaphore.Release();
            }
        }
    }
}
