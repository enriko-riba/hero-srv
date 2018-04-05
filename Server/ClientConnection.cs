using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using my_hero.ws;
using Newtonsoft.Json;

namespace my_hero.Server
{
    public class ClientConnection : WebSocketConnection
    {
        public ClientConnection(WebSocketHandler handler) : base(handler) { }

        public string NickName { get; set; }
        public string IdToken { get; set; }

        public override async Task ReceiveAsync(string message)
        {
            try
            {
                var clientMessage = JsonConvert.DeserializeObject<ClientMessage>(message);

                switch (clientMessage.Kind)
                {
                    case MessageKind.System:
                        await SendMessageAsync("ERROR: UNSUPPORTED TYPE");
                        break;

                    case MessageKind.Command:
                        await SendMessageAsync("OK: " + clientMessage.Data);
                        break;

                    case MessageKind.Chat:
                        await DispatchMessageAsync("Chat: " + clientMessage.Data);
                        break;

                    default:    //  for all other kinds
                        await SendMessageAsync("ERROR: UNSUPPORTED FORMAT");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await SendMessageAsync("ERROR: INVALID MESSAGE");
            }
        }

        private async Task DispatchMessageAsync(string message)
        {
            int counter = 0;
            var tasks = new Task[Handler.Connections.Count - 1];
            foreach (var c in Handler.Connections.Cast<ClientConnection>())
            {
                if (c != this)
                {
                    tasks[counter++] = c.SendMessageAsync(message);
                }
            }
            await Task.WhenAll(tasks);
        }

        private ClientConnection FindClient(string idToken)
        {
            var client = Handler.Connections.FirstOrDefault(m => ((ClientConnection)m).IdToken == idToken);
            return client as ClientConnection;
        }

        private class ClientMessage
        {
            public MessageKind Kind { get; set; }

            public string Data { get; set; }
        }

        private enum MessageKind
        {
            Invalid,
            System,
            Command,
            Chat
        }
    }

}