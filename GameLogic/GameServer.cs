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

        protected override void OnProcessState(PlayerData pd, long ellapsed, bool shouldSync)
        {

            var city = pd.City;

            var seconds = (float)ellapsed / 1000f;

            city.resources.food += city.production.food * seconds;
            city.resources.wood += city.production.wood * seconds;
            city.resources.stone += city.production.stone * seconds;
        }

        protected override void OnProcessRequest(PlayerData pd, ref RpgMessage msg)
        {
            Response r = new Response()
            {
                Created = msg.ClientTime,
                Tick = tick,
                Cid = msg.Cid,
                TargetKind = TargetKind.All
            };

            //  TODO: implement
            switch (msg.RpgType)
            {
                case RpgType.NullCommand:
                    r.TargetKind = TargetKind.TargetList;
                    r.Targets = new string[] { msg.PlayerId };
                    r.Data = $"CMD {msg.RpgType}: {msg.Data}";
                    break;

                case RpgType.Chat:
                    r.Data = $"{ msg.PlayerId}: {msg.Data}";
                    r.TargetKind = TargetKind.TargetAllExcept;
                    r.Targets = new string[] { msg.PlayerId };
                    break;

                case RpgType.StartBuilding:
                    r = ProcessStartBuilding(pd, ref msg);
                    break;
                default:
                    break;
            }
            responseBuffer.Enqueue(r);
        }

        #region Actions
        private Response ProcessStartBuilding(PlayerData pd, ref RpgMessage msg)
        {
            var cmdData = msg.Data.Split("|");
            if (!int.TryParse(cmdData[0], out int index))
            {
                return CreateErrorResponse(ref msg, "StartBuilding: Invalid slot index");
            }
            if (!int.TryParse(cmdData[1], out int buildingId))
            {
                return CreateErrorResponse(ref msg, "StartBuilding: Invalid building id");
            }
            if(pd.City.buildings[index] != null)
            {
                return CreateErrorResponse(ref msg, "StartBuilding: slot not empty");
            }

            // check resources
            var building = DataFactory.GetBuilding(buildingId);
            var isOk = building.Cost.food <= pd.City.resources.food &&
                       building.Cost.wood <= pd.City.resources.wood &&
                       building.Cost.stone <= pd.City.resources.stone;
            if (!isOk)
            {
                return CreateErrorResponse(ref msg, "StartBuilding: no resources");
            }

            pd.City.resources -= building.Cost;
            pd.City.buildings[index] = building;
            building.BuildTimeLeft = building.BuildTime * 1000;
            var data = JsonConvert.SerializeObject(new {
                slot = index,
                building = building
            });
            var r = CreateResponse(ref msg);
            r.Data = $"CMDR:{RpgType.StartBuilding}{data}";
            return r;
        }
        #endregion

        #region Messages
        private Response CreateResponse(ref RpgMessage msg)
        {
            Response r = new Response()
            {
                Tick = tick,
                Created = msg.ClientTime,
                Cid = msg.Cid,
                TargetKind = TargetKind.TargetList,
                Targets = new string[] { msg.PlayerId }
            };
            return r;
        }

        private Response CreateErrorResponse(ref RpgMessage msg, string error)
        {
            Response r = CreateResponse(ref msg);           
            r.Data = error;
            return r;
        }

        private RpgMessage ParseCommand(ref ClientMessage cm, string playerId)
        {
            //  TODO: define data structure, parse cmd, parse payload
            var msg = new RpgMessage()
            {
                PlayerId = playerId,
                Cid = cm.Cid,
                ClientTime = cm.Created,
            };

            var cmd = cm.Data.Substring(0, 2);
            if (int.TryParse(cmd, out int code))
            {
                var type = (RpgType)code;
                msg.RpgType = type;
                msg.Data = cm.Data.Substring(2);
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
                Data = cm.Data,
                RpgType = RpgType.Chat
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
