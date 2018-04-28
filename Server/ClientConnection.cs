namespace ws_hero.Server
{
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using ws_hero.GameLogic;
    using ws_hero.Messages;
    using ws_hero.sockets;

    public class ClientConnection : WebSocketConnection
    {
        private readonly GameServer srv;
        public ClientConnection(GameServer srv)
        {
            this.srv = srv;
        }

        public string PlayerId { get; set; }
        public string IdToken { get; set; }

        /// <summary>
        /// Parses client messages, converts them to RpgMessage objects and enqueues them.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override async Task ReceiveAsync(string message)
        {
            try
            {
                if (!srv.IsRunning)
                {
                    await SendMessageAsync("ERROR: SERVER DOWN");
                    return;
                }
                await srv.ParseClientMessage(this, message);                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await SendMessageAsync($"ERROR: {ex.Message}");
            }
        }
    }
}