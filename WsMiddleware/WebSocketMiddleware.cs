using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ws_hero.sockets
{
    /// <summary>
    /// This should be the last middleware in the pipeline when using websockets
    /// </summary>
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private WebSocketHandler _webSocketHandler { get; set; }

        public WebSocketMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var connection = await _webSocketHandler.OnConnected(context);
                if (connection != null)
                {
                    await _webSocketHandler.ListenConnection(connection);
                }                
            }
        }
    }
}
