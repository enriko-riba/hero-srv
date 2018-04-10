using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ws_hero.DAL;
using ws_hero.Server;

namespace ws_hero.sockets
{
    public class WebSocketHandler
    {
        private const int BUFFER_SIZE = 1024 * 8;

        //  TODO: DI?
        private ConnectionManager connMngr = SimpleServer.Instance.ConnMngr as ConnectionManager;

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
                if (connection != null && connection.WebSocket.State != WebSocketState.Open)
                {
                    connMngr.Remove(connection);
                    connection = null;
                }

                //------------------------------------
                //  create new connection if needed
                //------------------------------------
                if (connection == null)
                {
                    //-------------------------------
                    //  verify google id_token
                    //-------------------------------
                    User user = new User();
                    try
                    {
                        var tr = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken);
                        user = new User()
                        {
                            Email = tr.Email,
                            FirstName = tr.GivenName,
                            LastName = tr.FamilyName,
                            DisplayName = tr.Name,
                            PictureURL = tr.Picture                            
                        };
                        await SimpleServer.Instance.UpsertUserAsync(user);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        context.Response.StatusCode = 401;
                        return null;
                    }


                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();


                    //  TODO: fetch user data
                    connection = new ClientConnection()
                    {
                        PlayerId = user.Email,
                        IdToken = idToken,
                        WebSocket = webSocket
                    };

                    connMngr.Add(connection);
                }
                return connection;
            }

            context.Response.StatusCode = 404;
            return null;
        }
    }
}
