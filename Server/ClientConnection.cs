namespace ws_hero.Server
{
    using Newtonsoft.Json;
    using System;
    using System.Threading.Tasks;
    using ws_hero.Messages;
    using ws_hero.sockets;

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
                    case ClientMessageKind.Chat:
                        //await SendMessageAsync("OK: " + clientMessage.Data);
                        var rpgMsg = RpgMessage.FromClientData(PlayerId, ref clientMessage);
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


    }
}