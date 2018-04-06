namespace my_hero.Server
{
    using System;
    using System.Linq;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    public class SimpleServer
    {
        private static readonly SimpleServer singleton = new SimpleServer();

        private ConnectionManager<ClientConnection> connMngr;

        private ulong tick = 0;
        private bool isRunning;
        private Task mainLoopTask;

        private Dictionary<string, Player> players = new Dictionary<string, Player>();

        private object bufferLock = new Object();
        private Queue<Message> msgBuff1 = new Queue<Message>(1024);
        private Queue<Message> msgBuff2 = new Queue<Message>(1024);
        private Queue<Message> messageBuffer;


        private ConcurrentQueue<Response> responseBuffer = new ConcurrentQueue<Response>();

        public SimpleServer()
        {
            //  TODO: implement real mapper that returns playerId from token/DB
            int playerCount = 0;
            Func<ClientConnection, string> mapper = (ClientConnection c) => (++playerCount).ToString();
            connMngr = new ConnectionManager<ClientConnection>(mapper);
        }

        public static SimpleServer Instance { get { return singleton; } }

        public ConnectionManager<ClientConnection> ConnMngr { get => connMngr; }

        public bool IsRunning { get { return this.isRunning; } }

        public void Start()
        {
            if (isRunning) throw new Exception("Already running");

            Console.WriteLine("Starting server...");
            SwapBuffers();
            this.isRunning = true;
            this.mainLoopTask = Task.Run((Action)MainLoop);
            Console.WriteLine("server started!");
        }

        public void Stop()
        {
            Console.WriteLine("Stopping server...");
            this.isRunning = false;
            if (this.mainLoopTask != null)
            {
                Console.WriteLine("waiting for loop thread termination...");
                this.mainLoopTask.Wait();
                Console.WriteLine("loop thread terminated.");
            }
            Console.WriteLine("Server stopped!");
        }

        public void AddMessage(ref Message m)
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

            while (isRunning)
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
                hasItems = this.messageBuffer.TryDequeue(out Message msg);
                if (hasItems)
                {
                    ProcessRequest(ref msg);
                }
            } while (hasItems);
        }

        private void ProcessRequest(ref Message msg)
        {
            Response r = new Response()
            {
                Tick = tick,
                Cid = msg.Cid,
                Data = msg.Data,
                TargetKind = TargetKind.All
            };

            //  TODO: implement
            switch (msg.Command)
            {
                case Command.NullCommand:                 
                    responseBuffer.Enqueue(r);
                    break;

                case Command.Chat:
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
                        kvp.Value.SendMessageAsync(msg.Data).ContinueWith(c => c);
                    }
                }
            } while (hasItems);
        }
    }
}