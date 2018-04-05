using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using my_hero.ws;

namespace my_hero.Server
{
    public class ClientHandler : WebSocketHandler
    {
        protected override int BufferSize { get => 1024 * 8; }

        public override async Task<WebSocketConnection> OnConnected(HttpContext context)
        {
            var idToken = context.Request.Query["idToken"];
            var name = context.Request.Query["name"];
            if (!string.IsNullOrEmpty(idToken))
            {
                //  todo: implement internal mapper from idToken to id so we don't need to maintain the huge idToken string
                var connection = Connections.FirstOrDefault(m => ((ClientConnection)m).IdToken == idToken);

                if (connection == null)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    
                    //  todo: verify token & fetch user data
                    connection = new ClientConnection(this)
                    {
                        NickName = name,
                        IdToken = idToken,
                        WebSocket = webSocket
                    };

                    Connections.Add(connection);
                }

                return connection;
            }

            return null;
        }
    }
}
