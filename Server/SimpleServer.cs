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
    using ws_hero.DAL;

    public class SimpleServer
    {
        private static readonly SimpleServer singleton = new SimpleServer();

        private ConnectionManager connMngr;

        private ulong tick = 0;
        private bool isRunning;
        private Task mainLoopTask;

        private Dictionary<string, User> players = new Dictionary<string, User>();

        private object bufferLock = new Object();
        private Queue<RpgMessage> msgBuff1 = new Queue<RpgMessage>(1024);
        private Queue<RpgMessage> msgBuff2 = new Queue<RpgMessage>(1024);
        private Queue<RpgMessage> messageBuffer;


        private ConcurrentQueue<Response> responseBuffer = new ConcurrentQueue<Response>();
        private CosmosRepo cr = new CosmosRepo();

        public SimpleServer()
        {                    
            connMngr = new ConnectionManager();            
        }

        public static SimpleServer Instance { get => singleton; }

        public ConnectionManager ConnMngr { get => connMngr; }
        
        public bool IsRunning { get => isRunning; private set => isRunning = value; }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public async Task Start()
        {
            if (IsRunning) throw new Exception("Already running");

            Console.WriteLine("Starting server...");
            await cr.InitAsync();

            SwapBuffers();
            IsRunning = true;
            mainLoopTask = Task.Run((Action)MainLoop);
            Console.WriteLine("server started!");
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
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


        /// <summary>
        /// Updates or inserts the given user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task UpsertUserAsync(User user)
        {
            var usr = await cr.CreateUserIfNotExistsAsync(user);
            
            //  if null its a new user so we set it to active
            if (usr.IsActive == null) usr.IsActive = true;

            if(usr.IsActive.Value && usr.GameData == null)
                usr.GameData = new GameData();

            //  TODO: implement new game data hook for initializing any game state

            this.players[user.Id] = user;
        }

        /// <summary>
        /// Enqueues a new message for dispatching.
        /// </summary>
        /// <param name="m"></param>
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
                ProcessState();
                DispatchResponses();

                var tickEnd = sw.ElapsedMilliseconds;
                var duration = tickEnd - tickStart;
                Thread.Sleep(200);
            }

            Console.WriteLine("Main loop ended");
        }


        private void ProcessState()
        {

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
                    r.TargetKind = TargetKind.TargetAllExcept;
                    r.Targets = new string[] { msg.PlayerId };
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