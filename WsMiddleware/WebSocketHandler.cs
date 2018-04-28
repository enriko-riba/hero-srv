using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ws_hero.GameLogic;
using ws_hero.Server;

namespace ws_hero.sockets
{
    public class WebSocketHandler
    {
        private const int BUFFER_SIZE = 1024 * 8;

        //  TODO: DI?
        private ConnectionManager connMngr;
        private readonly GameServer srv;

        public WebSocketHandler(GameServer srv)
        {
            this.srv = srv;
            connMngr = srv.ConnMngr as ConnectionManager;
        }

        public async Task ListenConnection(WebSocketConnection connection)
        {
            var buffer = new byte[BUFFER_SIZE];
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

                //-------------------------------
                //  remove stale connections
                //-------------------------------
                if (connection != null)
                {
                    if (connection.WebSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await connection.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "new connection accepted!", CancellationToken.None);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }

                    connMngr.Remove(connection);
                    connection = null;
                }

                //-------------------------------
                //  verify google id_token
                //-------------------------------
                string email;
                try
                {
                    var tr = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken);
                    email = tr.Email;
                    var user = await srv.SignInUserAsync(email, tr.FamilyName, tr.GivenName, tr.Name, tr.Picture);

                    if (user != null)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        connection = new ClientConnection(srv)
                        {
                            PlayerId = email,
                            IdToken = idToken,
                            WebSocket = webSocket
                        };
                        connMngr.Add(connection);
                        srv.ConnectionAdded(user);                        
                    }
                    else
                    {
                        context.Response.StatusCode = 403;
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    
                    const string JWT= "JWT";
                    if(ex.Message.StartsWith(JWT, StringComparison.InvariantCultureIgnoreCase))
                    {
                        context.Response.StatusCode = 498;  //  INVALID TOKEN
                    }
                    else
                    {
                        context.Response.StatusCode = 401;  //  UNAUTHORIZED
                    }
                    return null;
                }
                return connection;
            }

            context.Response.StatusCode = 404;
            return null;
        }
    }
}
