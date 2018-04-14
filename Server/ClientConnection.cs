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
        public string PlayerId { get; set; }
        public string IdToken { get; set; }

        /// <summary>
        /// Parses client messages, converts them to RpgMessage objects and enqueues them.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override async Task ReceiveAsync(string message)
        {
            var server = GameServer.Instance;
            try
            {
                if (!server.IsRunning)
                {
                    await SendMessageAsync("ERROR: SERVER DOWN");
                    return;
                }

                RpgMessage rpgMsg;
                var clientMessage = JsonConvert.DeserializeObject<ClientMessage>(message);
                switch (clientMessage.Kind)
                {
                    case ClientMessageKind.System:
                        await SendMessageAsync("ERROR: UNSUPPORTED TYPE");
                        break;

                    case ClientMessageKind.Command:
                        rpgMsg = RpgMessage.FromClientMessage(PlayerId, ref clientMessage);
                        server.EnqueueRpgMessage(ref rpgMsg);
                        break;

                    case ClientMessageKind.Chat:
                        rpgMsg = RpgMessage.FromClientMessage(PlayerId, ref clientMessage);
                        server.EnqueueRpgMessage(ref rpgMsg);
                        break;

                    default:    //  for all other kinds
                        await SendMessageAsync("ERROR: UNSUPPORTED FORMAT");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await SendMessageAsync($"ERROR: {ex.Message}");
            }
        }
    }
}