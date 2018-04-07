using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ws_hero.Server;

namespace ws_hero.sockets
{
    public class WebSocketHandler
    {
        private const int BufferSize = 1024 * 8;

        //  TODO: DI?
        private ConnectionManager connMngr = SimpleServer.Instance.ConnMngr as ConnectionManager;

        //  TODO: remove after implementing DB
        private static int playerIdCounter;

        public async Task ListenConnection(WebSocketConnection connection)
        {
            var buffer = new byte[BufferSize];

            while (connection.WebSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await connection.WebSocket.ReceiveAsync(
                        buffer: new ArraySegment<byte>(buffer),
                        cancellationToken: CancellationToken.None);

                    try
                    {
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            await connection.ReceiveAsync(message);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await OnDisconnected(connection);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public async Task OnDisconnected(WebSocketConnection connection)
        {
            if (connection != null)
            {
                try
                {
                    var cc = connMngr.Connections.FirstOrDefault(m => m as WebSocketConnection == connection);
                    connMngr.Remove(cc);
                    
                    await connection.WebSocket.CloseAsync(
                        closeStatus: WebSocketCloseStatus.NormalClosure,
                        statusDescription: "Closed by the WebSocketHandler",
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public async Task<WebSocketConnection> OnConnected(HttpContext context)
        {
            var idToken = context.Request.Query["idToken"];
            if (!string.IsNullOrEmpty(idToken))
            {
                var connection = connMngr.FindByToken(idToken);
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
