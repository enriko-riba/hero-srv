using Microsoft.AspNetCore.Http;
using my_hero.Server;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace my_hero.ws
{
    public abstract class WebSocketHandler
    {
        protected abstract int BufferSize { get; }

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

        public virtual async Task OnDisconnected(WebSocketConnection connection)
        {
            if (connection != null)
            {
                try
                {
                    var cc = SimpleServer.Instance.ConnMngr.Connections.FirstOrDefault(m => m as WebSocketConnection == connection);
                    SimpleServer.Instance.ConnMngr.Remove(cc);
                    
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

        public abstract Task<WebSocketConnection> OnConnected(HttpContext context);
    }
}
