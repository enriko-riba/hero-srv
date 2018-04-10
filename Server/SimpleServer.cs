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

        //private object bufferLock = new Object();
        private Queue<RpgMessage>[] messageBuffers = new [] {new Queue<RpgMessage>(1024), new Queue<RpgMessage>(1024) };
        private int writeBuffer = 0;
        private int readBuffer = 1;


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
            var userList = cr.GetActiveUsers();
            foreach (var u in userList)
            {
                this.players.Add(u.Id, u);
            }

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
        public async Task<User> SignInUserAsync(string email, string lastName, string firstName, string displayName, string photoUrl)
        {
            var usr = await cr.GetUserAsync(email);

            //-------------------------
            //  bail out if inactive
            //-------------------------
            if (usr!=null && !usr.IsActive.Value)
            {
                this.players.Remove(usr.Id);
                return null;
            }

            //-------------------------
            //  init new users
            //-------------------------
            if (usr == null) 
            {
                usr = new User()
                {
                    Email = email,
                    LastName = lastName,
                    FirstName = firstName,
                    DisplayName = displayName,
                    PictureURL = photoUrl,
                    IsActive = true,
                    GameData = new GameData()
                };
                //  TODO: implement new game data hook for initializing any game state
            }

            usr = await cr.SaveUserAsync(usr);
            this.players[usr.Id] = usr;
            return usr;
        }

        /// <summary>
        /// Enqueues a new message for dispatching.
        /// </summary>
        /// <param name="m"></param>
        public void AddMessage(ref RpgMessage m)
        {
            this.messageBuffers[writeBuffer].Enqueue(m);
        }

        /// <summary>
        /// Swaps the msgBuff1 and msgBuff2.
        /// </summary>
        private void SwapBuffers()
        {
            writeBuffer = ++writeBuffer % 2;
            readBuffer = ++readBuffer % 2;
        }

        private void MainLoop()
        {
            Console.WriteLine("Main loop started");
            var sw = new Stopwatch();
            sw.Start();

            const int SLEEP_MILLISECONDS = 200;
            const int SYNC_MILLISECONDS = 5000;
            long tickEnd = sw.ElapsedMilliseconds;
            long ellapsed, tickStart;

            long lastStateSync = 0;
            while (IsRunning)
            {
                tick++;
                tickStart = sw.ElapsedMilliseconds;
                ellapsed = tickStart - tickEnd;

                SwapBuffers();
                ProcessRequests();

                bool shouldSync = (tickStart - lastStateSync > SYNC_MILLISECONDS);
                ProcessState(ellapsed, shouldSync);
                if (shouldSync) lastStateSync = tickStart;

                DispatchResponses();

                tickEnd = sw.ElapsedMilliseconds;
                Thread.Sleep(SLEEP_MILLISECONDS);
            }

            Console.WriteLine("Main loop ended");
        }

        /// <summary>
        /// Processes time based game state.
        /// </summary>
        /// <param name="ellapsed"></param>
        private async Task ProcessState(long ellapsed, bool shouldSync)
        {
            foreach(var kvp in this.players)
            {
                var user = kvp.Value;
                var city = user.GameData.City;

                var seconds = ellapsed / 1000;

                city.food += city.prodFood * seconds;
                city.wood += city.prodWood * seconds;
                city.stone += city.prodStone * seconds;

                //  send data to client if needed
                if(shouldSync)
                {
                    var data = Newtonsoft.Json.JsonConvert.SerializeObject(user.GameData);
                    Response r = new Response()
                    {
                        Tick = this.tick,
                        Cid = 0,
                        Data = $"SYNC:{data}",
                        TargetKind = TargetKind.TargetList,
                        Targets = new string[] { user.Id }
                    };
                    responseBuffer.Enqueue(r);

                    var task = cr.SaveUserAsync(user);
                    task.ContinueWith((t) =>
                    {
                        this.players[t.Result.Id] = t.Result;
                    });
                }             
            }
        }

        /// <summary>
        /// Handles incomming client requests
        /// </summary>
        private void ProcessRequests()
        {
            var hasItems = false;
            do
            {
                hasItems = this.messageBuffers[readBuffer].TryDequeue(out RpgMessage msg);
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