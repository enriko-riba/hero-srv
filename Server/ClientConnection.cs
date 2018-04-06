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
        public int PlayerId { get; set; }
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

                var clientMessage = JsonConvert.DeserializeObject<ClientMessage>(message);

                switch (clientMessage.Kind)
                {
                    case ClientMessageKind.System:
                        await SendMessageAsync("ERROR: UNSUPPORTED TYPE");
                        break;

                    case ClientMessageKind.Command:
                        await SendMessageAsync("OK: " + clientMessage.Data);
                        var m = Message.FromClientData(PlayerId, clientMessage.Kind, clientMessage.Data);
                        SimpleServer.Instance.AddMessage(ref m);
                        break;

                    case ClientMessageKind.Chat:
                        //await DispatchMessageAsync("Chat: " + clientMessage.Data);
                        var cm = Message.FromClientData(PlayerId, clientMessage.Kind, clientMessage.Data);
                        SimpleServer.Instance.AddMessage(ref cm);
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

        //private async Task DispatchMessageAsync(string message)
        //{
        //    int counter = 0;
        //    var tasks = new Task[Handler.Connections.Count - 1];
        //    foreach (var c in Handler.Connections.Cast<ClientConnection>())
        //    {
        //        if (c != this)
        //        {
        //            tasks[counter++] = c.SendMessageAsync(message);
        //        }
        //    }
        //    await Task.WhenAll(tasks);
        //}

        //private ClientConnection FindClient(string idToken)
        //{
        //    var client = Handler.Connections.FirstOrDefault(m => ((ClientConnection)m).IdToken == idToken);
        //    return client as ClientConnection;
        //}

        private class ClientMessage
        {
            public int Cid { get; set; }
            public ClientMessageKind Kind { get; set; }

            public string Data { get; set; }
        }        
    }
}