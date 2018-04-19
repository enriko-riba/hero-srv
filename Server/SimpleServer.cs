namespace ws_hero.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ws_hero.DAL;
    using ws_hero.Messages;

    public abstract class SimpleServer<T> where T : class, new()
    {
        private ConnectionManager connMngr = new ConnectionManager();
        private bool isRunning;
        private Task mainLoopTask;
        private Dictionary<string, User<T>> players = new Dictionary<string, User<T>>();
        private Queue<RpgMessage>[] messageBuffers = new [] {new Queue<RpgMessage>(1024), new Queue<RpgMessage>(1024) };
        private int writeBuffer = 0;
        private int readBuffer = 1;

        protected ConcurrentQueue<Response> responseBuffer = new ConcurrentQueue<Response>();
        protected CosmosRepo<T> cr = new CosmosRepo<T>();
        protected ulong tick = 0;

        public ConnectionManager ConnMngr { get => connMngr; }
        
        /// <summary>
        /// Returns true if the server is running.
        /// </summary>
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
                OnUserLoaded(u);
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
        public async Task<User<T>> SignInUserAsync(string email, string lastName, string firstName, string displayName, string photoUrl)
        {
            var usr = await cr.GetUserAsync(email);

            //-------------------------
            //  init new users
            //-------------------------
            if (usr == null) 
            {
                usr = new User<T>()
                {
                    Email = email,
                    LastName = lastName,
                    FirstName = firstName,
                    DisplayName = displayName,
                    PictureURL = photoUrl,
                    IsActive = true,
                    GameData = new T()
                };

                OnNewUserAdded(usr);
            }
            else if(!usr.IsActive.Value)
            {
                //-------------------------
                //  bail out inactive
                //-------------------------
                this.players.Remove(usr.Id);
                return null;
            }

            if (players.ContainsKey(usr.Id))
            {
                usr = players[usr.Id];
            }
            else
            {
                OnUserLoaded(usr);
            }

            usr = await cr.SaveUserAsync(usr); // TODO: rethink last login timestamp. This save() is only to trigger _ts property update which is mapped into User.LastLogin
            this.players[usr.Id] = usr;

            return usr;
        }

        /// <summary>
        /// Enqueues a RpgMessage for processing.
        /// </summary>
        /// <param name="m"></param>
        public void EnqueueRpgMessage(ref RpgMessage m)
        {
            this.messageBuffers[writeBuffer].Enqueue(m);
        }

        /// <summary>
        /// Add all player init logic here.
        /// </summary>
        /// <param name="user"></param>
        public abstract void ConnectionAdded(User<T> user);

        /// <summary>
        /// Generates and enqueues a sync message for the given user.
        /// </summary>
        /// <param name="user"></param>
        protected abstract void GenerateSyncMessage(User<T> user);

        /// <summary>
        /// Determins if the game state should be synced to clients.
        /// </summary>
        /// <param name="tickStart"></param>
        /// <param name="lastStateSync"></param>
        /// <returns></returns>
        protected abstract bool ShouldSync(long tickStart, long lastStateSync);

        protected abstract void OnProcessState(User<T> user, int ellapsedMilliseconds);

        protected abstract void OnProcessRequest(User<T> user, ref RpgMessage msg);

        protected abstract void OnUserLoaded(User<T> user);

        protected abstract void OnNewUserAdded(User<T> user);

        /// <summary>
        /// Processes time based game state.
        /// </summary>
        /// <param name="ellapsedMilliseconds"></param>
        private void ProcessPlayerState(int ellapsedMilliseconds)
        {
            var keys = this.players.Keys.ToArray();
            foreach (var key in keys)
            {
                var user = this.players[key];
                OnProcessState(user, ellapsedMilliseconds);

                //  send data to client if needed
                const int SYNC_INTERVAL = 2000;
                if (user.LastSync.AddMilliseconds(SYNC_INTERVAL) < DateTime.Now)
                    GenerateSyncMessage(user);                    

                //  save data to DB
                const int SAVE_INTERVAL = 15000;
                if (user.LastSave.AddMilliseconds(SAVE_INTERVAL) < DateTime.Now)
                {
                    var task = cr.SaveUserAsync(user);
                    //task.ContinueWith((t) =>
                    //{
                    //    Console.WriteLine("ContinueWith() {0}", Newtonsoft.Json.JsonConvert.SerializeObject(t.Result.GameData));
                    //    this.players[t.Result.Id] = t.Result;
                    //});
                }
            }
        }

        /// <summary>
        /// Handles incomming client requests.
        /// </summary>
        private void ProcessRpgMessages()
        {
            var hasItems = false;
            do
            {
                hasItems = this.messageBuffers[readBuffer].TryDequeue(out RpgMessage msg);
                if (hasItems)
                {
                    OnProcessRequest(this.players[msg.PlayerId], ref msg);
                }
            } while (hasItems);
        }
        
        /// <summary>
        /// Sends responses to clients.
        /// </summary>
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

        /// <summary>
        /// Swaps the read & write buffers.
        /// </summary>
        private void SwapBuffers()
        {
            writeBuffer = ++writeBuffer % 2;
            readBuffer = ++readBuffer % 2;
        }

        /// <summary>
        /// 
        /// </summary>
        private void MainLoop()
        {
            Console.WriteLine("Main loop started");
            var sw = new Stopwatch();
            sw.Start();

            const int SLEEP_MILLISECONDS = 200;
            long tickEnd = sw.ElapsedMilliseconds;
            long tickStart;
            int ellapsed;
            while (IsRunning)
            {
                tick++;
                tickStart = sw.ElapsedMilliseconds;
                ellapsed = (int)(tickStart - tickEnd);

                SwapBuffers();
                ProcessRpgMessages();
                ProcessPlayerState(ellapsed);

                DispatchResponses();    //  TODO: make background thread & event signal on message enqueue

                tickEnd = sw.ElapsedMilliseconds;
                Thread.Sleep(SLEEP_MILLISECONDS);
            }

            Console.WriteLine("Main loop ended");
        }
    }
}