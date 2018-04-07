namespace ws_hero.Server
{
    using System;
    using System.Linq;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using ws_hero.Messages;

    public class SimpleServer
    {
        private static readonly SimpleServer singleton = new SimpleServer();

        private ConnectionManager connMngr;

        private ulong tick = 0;
        private bool isRunning;
        private Task mainLoopTask;

        private Dictionary<string, Player> players = new Dictionary<string, Player>();

        private object bufferLock = new Object();
        private Queue<RpgMessage> msgBuff1 = new Queue<RpgMessage>(1024);
        private Queue<RpgMessage> msgBuff2 = new Queue<RpgMessage>(1024);
        private Queue<RpgMessage> messageBuffer;


        private ConcurrentQueue<Response> responseBuffer = new ConcurrentQueue<Response>();

        public SimpleServer()
        {
            //  TODO: implement real mapper that returns playerId from token/DB
            int playerCount = 0;
            Func<ClientConnection, string> mapper = (ClientConnection c) => (++playerCount).ToString();
            connMngr = new ConnectionManager(mapper);
        }

        public static SimpleServer Instance { get { return singleton; } }

        public ConnectionManager ConnMngr { get => connMngr; }


        public bool IsRunning { get => isRunning; private set => isRunning = value; }

        public void Start()
        {
            if (IsRunning) throw new Exception("Already running");

            Console.WriteLine("Starting server...");
            SwapBuffers();
            IsRunning = true;
            mainLoopTask = Task.Run((Action)MainLoop);
            Console.WriteLine("server started!");
        }

        public void Stop()
        {
            Console.WriteLine("Stopping server...");
            IsRunning = false;
            if (this.mainLoopTask != null)
            {
                Console.WriteLine("waiting for loop thread termination...");
                this.mainLoopTask.Wait();
                Console.WriteLine("loop thread terminated.");
            }
            Console.WriteLine("Server stopped!");
        }

        public void AddMessage(ref RpgMessage m)
        {
            this.messageBuffer.Enqueue(m);
        }


        private void SwapBuffers()
        {
            this.messageBuffer = this.messageBuffer == this.msgBuff1 ? this.msgBuff2 : this.msgBuff1;
        }

        private void MainLoop()
        {
            Console.WriteLine("Main loop started");
            var sw = new Stopwatch();
            sw.Start();

            while (IsRunning)
            {
                var tickStart = sw.ElapsedMilliseconds;
                tick++;

                SwapBuffers();
                ProcessRequests();
                DispatchResponses();

                var tickEnd = sw.ElapsedMilliseconds;
                var duration = tickEnd - tickStart;
                Thread.Sleep(200);
            }

            Console.WriteLine("Main loop ended");
        }


        private void ProcessRequests()
        {
            var hasItems = false;
            do
            {
                hasItems = this.messageBuffer.TryDequeue(out RpgMessage msg);
                if (hasItems)
                {
                    ProcessRequest(ref msg);
                }
            } while (hasItems);
        }

        private void ProcessRequest(ref RpgMessage msg)
        {
            Response r = new Response()
            {
                Tick = tick,
                Cid = msg.Cid,
                TargetKind = TargetKind.All
            };

            //  TODO: implement
            switch (msg.RpgType)
            {
                case RpgType.NullCommand:
                    r.Data = $"CMD {msg.RpgType}: {msg.Data}";
                    responseBuffer.Enqueue(r);
                    break;

                case RpgType.Chat:
                    r.Data = $"{ msg.PlayerId}: {msg.Data}";
                    responseBuffer.Enqueue(r);
                    break;

                default:
                    break;
            }
        }

        private void DispatchResponses()
        {
            var hasItems = false;
            do
            {
                hasItems = this.responseBuffer.TryDequeue(out Response msg);
                if (hasItems)
                {
                    Func<KeyValuePair<string, ClientConnection>, bool> predicate;
                    if (msg.TargetKind == TargetKind.TargetAllExcept)
                        predicate = (kvp) => !msg.Targets.Any(kvp.Key.Contains);
                    else if (msg.TargetKind == TargetKind.TargetList)
                        predicate = (kvp) => msg.Targets.Any(kvp.Key.Contains);
                    else
                        predicate = (kvp) => true;

                    var list = connMngr.GetAll(predicate);
                    foreach (var kvp in list)
                    {
                        var result = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                        kvp.Value.SendMessageAsync(result).ContinueWith(c => c);
                    }
                }
            } while (hasItems);
        }
    }
}