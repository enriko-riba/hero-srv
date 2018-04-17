using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using ws_hero.DAL;
using ws_hero.Messages;
using ws_hero.Server;

namespace ws_hero.GameLogic
{
    public class GameServer : SimpleServer<PlayerData>
    {
        private const int SYNC_MILLISECONDS = 5000;

        private static readonly GameServer singleton = new GameServer();
        public static GameServer Instance { get => singleton; }

        public GameServer()
        {
        }

       

        protected override bool ShouldSync(long tickStart, long lastStateSync) => (tickStart - lastStateSync > SYNC_MILLISECONDS);

        protected override void OnProcessState(User<PlayerData> user, long ellapsed, bool shouldSync)
        {
            var city = (user.GameData as PlayerData).City;

            var seconds = (float)ellapsed / 1000f;

            city.resources.food += city.production.food * seconds;
            city.resources.wood += city.production.wood * seconds;
            city.resources.stone += city.production.stone * seconds;
        }

        protected override void OnProcessRequest(ref RpgMessage msg)
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

        #region Messages
        private RpgMessage ParseCommand(ref ClientMessage cm, string playerId)
        {
            //  TODO: define data structure, parse cmd, parse payload
            var msg = new RpgMessage()
            {
                PlayerId = playerId,
                Cid = cm.Cid,
                ClientTime = cm.Created,
                Data = cm.Data
            };

            var cmd = cm.Data.Substring(0, 2);
            if (int.TryParse(cmd, out int code))
            {
                var type = (RpgType)code;
                switch (type)
                {
                    case RpgType.Login:
                        msg.RpgType = RpgType.Login;
                        break;

                    default:
                        msg.RpgType = RpgType.NullCommand;
                        break;
                }
                return msg;
            }
            throw new Exception("INVALID CODE");
        }

        private RpgMessage ParseChat(ref ClientMessage cm, string playerId)
        {
            var msg = new RpgMessage()
            {
                PlayerId = playerId,
                Cid = cm.Cid,
                ClientTime = cm.Created,
                Data = cm.Data
            };
            return msg;
        }

        public async Task ParseClientMessage(ClientConnection cc, string message)
        {
            RpgMessage rpgMsg;
            var clientMessage = JsonConvert.DeserializeObject<ClientMessage>(message);
            switch (clientMessage.Kind)
            {
                case ClientMessageKind.System:
                    await cc.SendMessageAsync("ERROR: UNSUPPORTED TYPE");
                    break;

                case ClientMessageKind.Command:
                    rpgMsg = ParseCommand(ref clientMessage, cc.PlayerId);
                    EnqueueRpgMessage(ref rpgMsg);
                    break;

                case ClientMessageKind.Chat:
                    rpgMsg = ParseChat(ref clientMessage, cc.PlayerId);
                    EnqueueRpgMessage(ref rpgMsg);
                    break;

                default:    //  for all other kinds
                    await cc.SendMessageAsync("ERROR: UNSUPPORTED FORMAT");
                    break;
            }
        }

        public override void ConnectionAdded(User<PlayerData> user)
        {
            user.GameData.City.RecalculateProduction();
            GenerateWorldInitMessage(user);
            GenerateSyncMessage(user);
        }

        public void GenerateWorldInitMessage(User<PlayerData> user)
        {
            var o = new
            {
                BuildingData = DataFactory.GetBuildings(),
                ItemData = DataFactory.GetItems()
            };
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(o);
            Response r = new Response()
            {
                Tick = this.tick,
                Cid = 0,
                Data = $"WINI:{data}",
                TargetKind = TargetKind.TargetList,
                Targets = new string[] { user.Id }
            };
            responseBuffer.Enqueue(r);
        }       

        /// <summary>
        /// Enqueues a sync message for the given user.
        /// </summary>
        /// <param name="user"></param>
        protected override void GenerateSyncMessage(User<PlayerData> user)
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
        }
        #endregion
    }
}
