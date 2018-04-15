using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ws_hero.sockets
{
    public static class Extensions
    {
        public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app, PathString path, WebSocketHandler handler)
        {
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketMiddleware>(handler));
        }

        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddSingleton(typeof(WebSocketHandler));
            return services;
        }
    }
}
