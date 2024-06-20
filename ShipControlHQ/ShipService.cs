using ShipControlCommon;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Text.Json;

namespace ShipControlHQ
{
    internal class ShipService : WebSocketBehavior, ThreadedQueue<Request>.IItemsHandler
    {
        private WebSocket? _client;
        private readonly ThreadedQueue<Request> _requests;

        public ShipService()
        {
            _requests = new(this);
        }

        protected override void OnOpen()
        {
            var sessionId = Context.QueryString["sessionId"];
            if (_client != null)
            {
                // Close the existing connection for this session
                _client.Close();
            }

            _client = this.Context.WebSocket;
            _requests.Pulse();
            Console.WriteLine($"Client connected to session {sessionId}");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            var sessionId = Context.QueryString["sessionId"];
            _client = null;
            Console.WriteLine($"Client disconnected from session {sessionId}");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var sessionId = Context.QueryString["sessionId"];
            Console.WriteLine($"Received from client in session {sessionId}: {e.Data}");
            var response = JsonSerializer.Deserialize<Response>(e.Data);
            if (response != null)
            {
                Console.WriteLine($"Received result for request '{response.Id}': {String.Join(", ", response.Result?.Output)}");
            }
        }

        bool ThreadedQueue<Request>.IItemsHandler.ShouldProcessItems()
        {
            return _client != null && _client.ReadyState == WebSocketState.Open;
        }

        void ThreadedQueue<Request>.IItemsHandler.HandleItem(Request request)
        {
            Console.WriteLine($"Dispatching request {request.Id}");
            Send(JsonSerializer.Serialize(request));
        }

        public void SendCommand(Command cmd)
        {
            _requests.Enqueue(new Request(cmd));
        }
    }
}
