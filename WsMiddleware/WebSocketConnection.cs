namespace ws_hero.sockets
{
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class WebSocketConnection
    {       
        public WebSocket WebSocket { get; set; }

        public virtual async Task SendMessageAsync(string message)
        {
            if (WebSocket.State != WebSocketState.Open) return;
            var arr = Encoding.UTF8.GetBytes(message);

            var buffer = new ArraySegment<byte>(
                    array: arr,
                    offset: 0,
                    count: arr.Length);

            await WebSocket.SendAsync(
                buffer: buffer,
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None
                );
        }

        public abstract Task ReceiveAsync(string message);
    }
}
