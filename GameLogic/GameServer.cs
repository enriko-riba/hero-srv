﻿namespace ws_hero.GameLogic
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ws_hero.DAL;
    using ws_hero.Messages;
    using ws_hero.Server;

    public class GameServer : SimpleServer<PlayerData>
    {
        private const int SYNC_MILLISECONDS = 5000;
        private static readonly GameServer singleton = new GameServer();
        public static GameServer Instance { get => singleton; }

        private List<int> finishedBuidlings = new List<int>(10);

        protected override bool ShouldSync(long tickStart, long lastStateSync) => (tickStart - lastStateSync > SYNC_MILLISECONDS);

        protected override void OnProcessState(User<PlayerData> user, int ellapsedMilliseconds)
        {
            //------------------------------------
            //  calc resource production
            //------------------------------------
            var seconds = (float)ellapsedMilliseconds / 1000f;
            var city = user.GameData.City;
            city.resources += city.production * seconds;
            city.resources.Clamp(city.storageCap);

            //------------------------------------
            //  handle buildings
            //------------------------------------
            finishedBuidlings.Clear();
            for (int i = 0; i< city.buildings.Length; i++)
            {
                var b = city.buildings[i];
                if (b != null && b.BuildTimeLeft > 0)
                {
                    b.BuildTimeLeft -= ellapsedMilliseconds;
                    if(b.BuildTimeLeft <= 0)
                    {
                        b.BuildTimeLeft = 0;
                        finishedBuidlings.Add(i);
                    }
                }
            }
            if ( finishedBuidlings.Any())
            {
                city.RecalculateProduction();
                //  TODO: send user array of finished building slots
            }
        }

        protected override void OnProcessRequest(User<PlayerData> user, ref RpgMessage msg)
        {
            Response r = new Response()
            {
                Created = msg.ClientTime,
                Tick = tick,
                Cid = msg.Cid,
                TargetKind = TargetKind.All
            };

            //  TODO: implement
            switch (msg.Kind)
            {
                case MessageKind.Command:
                    r.TargetKind = TargetKind.TargetList;
                    r.Targets = new string[] { msg.PlayerId };
                    r.Data = $"CMD {msg.Kind}: {msg.Data}";
                    break;

                case MessageKind.Chat:
                    r.Data = $"{ msg.PlayerId}: {msg.Data}";
                    r.TargetKind = TargetKind.TargetAllExcept;
                    r.Targets = new string[] { msg.PlayerId };
                    break;

                case MessageKind.StartBuilding:
                    r = ProcessStartBuilding(user.GameData, ref msg);
                    break;
                default:
                    break;
            }
            responseBuffer.Enqueue(r);
        }

        protected override void OnUserLoaded(User<PlayerData> user)
        {
            user.GameData.City.RecalculateProduction();
        }

        protected override void OnNewUserAdded(User<PlayerData> user)
        {
            user.GameData.City.resources.food = 50;
            user.GameData.City.resources.wood = 50;
            user.GameData.City.resources.stone = 10;
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
            if (pd.City.buildings[index] != null)
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
            var data = JsonConvert.SerializeObject(new
            {
                slot = index,
                building = building
            });
            var r = CreateResponse(ref msg);
            r.Data = $"CMDR:{(int)MessageKind.StartBuilding}|{data}";
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

        /// <summary>
        /// Validates structure and parameters of individual messages.
        /// </summary>
        /// <param name="cm"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        private RpgMessage ConvertToRpgMessage(ref ClientMessage cm, string playerId)
        {
            var rpgMsg = new RpgMessage()
            {
                PlayerId = playerId,
                Cid = cm.Cid,
                ClientTime = cm.Created,
                Kind = cm.Kind,
                Data = cm.Data
            };
            switch(cm.Kind)
            {
                case MessageKind.StartBuilding:
                    //  TODO: parse out request data
                    break;
                case MessageKind.Chat:
                    //  TODO: parse out request data
                    break;

                default: throw new Exception("INVALID CODE");
            }
            return rpgMsg;            
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

        public async Task ParseClientMessage(ClientConnection cc, string message)
        {
            var clientMessage = JsonConvert.DeserializeObject<ClientMessage>(message);
            if (clientMessage.Kind <= MessageKind.Command)
            {
                await cc.SendMessageAsync("ERROR: UNSUPPORTED TYPE");
                return;
            }
            else if (clientMessage.Kind > MessageKind.Chat)
            {
                await cc.SendMessageAsync("ERROR: UNSUPPORTED TYPE");
                return;
            }
            else
            { 
                var rpgMsg = ConvertToRpgMessage(ref clientMessage, cc.PlayerId);
                EnqueueRpgMessage(ref rpgMsg);
            }
        }

        public override void ConnectionAdded(User<PlayerData> user)
        {
            user.GameData.City.RecalculateProduction();
            GenerateWorldInitMessage(user);
            GenerateSyncMessage(user);
        }


        /// <summary>
        /// Enqueues a sync message for the given user.
        /// </summary>
        /// <param name="user"></param>
        protected override void GenerateSyncMessage(User<PlayerData> user)
        {
            //  TODO: do not enqueue if user is not connected (maybe add connection to user)
            var data = JsonConvert.SerializeObject(user.GameData);
            Response r = new Response()
            {
                Tick = this.tick,
                Cid = 0,
                Data = $"SYNC:{data}",
                TargetKind = TargetKind.TargetList,
                Targets = new string[] { user.Id }
            };
            responseBuffer.Enqueue(r);
            user.LastSync = DateTime.Now;
        }
        #endregion
    }
}
