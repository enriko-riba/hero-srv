namespace my_hero.Server
{
    using Microsoft.AspNetCore.Http;
    using my_hero.ws;
    using System.Linq;
    using System.Threading.Tasks;

    public class ClientHandler : WebSocketHandler
    {
        private SimpleServer server = SimpleServer.Instance;
        private ConnectionManager<ClientConnection> connMngr = SimpleServer.Instance.ConnMngr;

        protected override int BufferSize { get => 1024 * 8; }

        private static int playerIdCounter;

        public override async Task<WebSocketConnection> OnConnected(HttpContext context)
        {
            var idToken = context.Request.Query["idToken"];
            if (!string.IsNullOrEmpty(idToken))
            {
                //  TODO: implement internal mapper from idToken to id so we don't need to maintain the huge idToken string
                var connection = connMngr.Connections.FirstOrDefault(m => m.IdToken == idToken);

                if (connection == null)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    
                    //  TODO: verify token & fetch user data
                    connection = new ClientConnection()
                    {
                        PlayerId = ++playerIdCounter,   //  TODO: set to real id (from DB?)
                        IdToken = idToken,
                        WebSocket = webSocket
                    };

                    connMngr.Add(connection);
                }
                return connection;
            }
            return null;
        }
    }
}
