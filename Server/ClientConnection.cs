namespace ws_hero.Server
{
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using ws_hero.Messages;
    using ws_hero.sockets;

    public class ClientConnection : WebSocketConnection
    {
        public string PlayerId { get; set; }
        public string IdToken { get; set; }


        public override async Task ReceiveAsync(string message)
        {
            var server = SimpleServer.Instance;
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
                        SimpleServer.Instance.AddMessage(ref rpgMsg);
                        break;

                    case ClientMessageKind.Chat:
                        rpgMsg = RpgMessage.FromClientMessage(PlayerId, ref clientMessage);
                        SimpleServer.Instance.AddMessage(ref rpgMsg);
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