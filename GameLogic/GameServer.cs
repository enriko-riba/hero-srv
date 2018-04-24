namespace ws_hero.GameLogic
{
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ws_hero.DAL;
    using ws_hero.Messages;
    using ws_hero.Server;

    public class GameServer : SimpleServer<PlayerData>
    {
        private const int SYNC_MILLISECONDS = 15000;
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
            city.resources.IncreaseWithClamp(city.production * seconds, city.storageCap);

            //------------------------------------
            //  handle buildings
            //------------------------------------
            Building b;
            finishedBuidlings.Clear();
            for (int i = 0; i < city.buildings.Length; i++)
            {
                b = city.buildings[i];
                if (b != null && b.BuildTimeLeft > 0)
                {
                    b.BuildTimeLeft -= ellapsedMilliseconds;
                    var builder = city.builders.First(bu => bu.slot == i);  //  find corresponding builder
                    builder.buildTimeLeft = b.BuildTimeLeft;
                    if (b.BuildTimeLeft <= 0)
                    {
                        b.BuildTimeLeft = 0;
                        builder.buildTimeLeft = 0;
                        builder.slot = -1;
                        finishedBuidlings.Add(i);
                    }
                }
            }

            //  do we have finished buildings?
            if (finishedBuidlings.Any())
            {
                foreach (var i in finishedBuidlings)
                {
                    b = city.buildings[i];
                    b.Level++;
                }
                city.RecalculateProduction();
                user.LastSync = DateTime.MinValue;  //  this forces sync message dispatch to the user
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
                    user.LastSync = DateTime.MinValue;  //  this forces sync message dispatch to the user
                    break;

                case MessageKind.StartBuildingUpgrade:
                    r = ProcessStartBuildingUpgrade(user.GameData, ref msg);
                    user.LastSync = DateTime.MinValue;  //  this forces sync message dispatch to the user
                    break;

                case MessageKind.StartBuildingDestroy:
                    r = ProcessStartBuildingDestroy(user.GameData, ref msg);
                    user.LastSync = DateTime.MinValue;  //  this forces sync message dispatch to the user
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
            user.GameData.City.resources.food = 350;
            user.GameData.City.resources.wood = 350;
            user.GameData.City.resources.stone = 150;
            user.GameData.City.buildings = new Building[20];
            user.GameData.City.builders = new[] {
                new Builder() {
                    buildTimeLeft = 0,
                    slot = -1,
                    expires = DateTime.MaxValue
                }
            };
        }

        #region Actions

        private Response ProcessStartBuildingDestroy(PlayerData pd, ref RpgMessage msg)
        {
            var cmdData = msg.Data.Split("|");
            if (!int.TryParse(cmdData[0], out int slot))
            {
                return CreateErrorResponse(ref msg, "StartBuildingDestroy: invalid slot index");
            }
            if (!int.TryParse(cmdData[1], out int buildingId))
            {
                return CreateErrorResponse(ref msg, "StartBuildingDestroy: invalid building id");
            }
            if (pd.City.buildings[slot] == null)
            {
                return CreateErrorResponse(ref msg, "StartBuildingDestroy: slot is empty");
            }
            if (pd.City.buildings[slot].Id != buildingId)
            {
                return CreateErrorResponse(ref msg, "StartBuildingDestroy: building id mismatch");
            }
            if (pd.City.buildings[slot].BuildTimeLeft > 0)
            {
                return CreateErrorResponse(ref msg, "StartBuildingDestroy: already upgrading");
            }

            pd.City.resources += pd.City.buildings[slot].DestroyRefund;
            pd.City.buildings[slot] = null;
            pd.City.RecalculateProduction();

            var data = JsonConvert.SerializeObject(new
            {
                slot,
                pd.City.resources
            });
            var r = CreateResponse(ref msg);
            r.Data = $"CMDR:{(int)MessageKind.StartBuildingDestroy}|{data}";
            return r;
        }

        private Response ProcessStartBuildingUpgrade(PlayerData pd, ref RpgMessage msg)
        {
            var cmdData = msg.Data.Split("|");
            if (!int.TryParse(cmdData[0], out int slot))
            {
                return CreateErrorResponse(ref msg, "StartBuildingUpgrade: invalid slot index");
            }
            if (!int.TryParse(cmdData[1], out int buildingId))
            {
                return CreateErrorResponse(ref msg, "StartBuildingUpgrade: invalid building id");
            }
            if (pd.City.buildings[slot] == null)
            {
                return CreateErrorResponse(ref msg, "StartBuildingUpgrade: slot is empty");
            }
            if (pd.City.buildings[slot].Id != buildingId)
            {
                return CreateErrorResponse(ref msg, "StartBuildingUpgrade: building id mismatch");
            }
            if (pd.City.buildings[slot].BuildTimeLeft > 0)
            {
                return CreateErrorResponse(ref msg, "StartBuildingUpgrade: already upgrading");
            }
            // check resources
            var building = pd.City.buildings[slot];
            var isOk = building.UpgradeCost.food <= pd.City.resources.food &&
                       building.UpgradeCost.wood <= pd.City.resources.wood &&
                       building.UpgradeCost.stone <= pd.City.resources.stone;
            if (!isOk)
            {
                return CreateErrorResponse(ref msg, "StartBuildingUpgrade: no resources");
            }
            else
            {
                //---------------------------
                //  check builders
                //---------------------------
                if (!UseBuilder(pd, building, slot))
                    return CreateErrorResponse(ref msg, "StartBuildingUpgrade: no builder");
            }

            pd.City.resources -= building.UpgradeCost;
            pd.City.buildings[slot] = building;
            building.BuildTimeLeft = building.UpgradeTime;
            var data = JsonConvert.SerializeObject(new
            {
                slot,
                building
            });
            var r = CreateResponse(ref msg);
            r.Data = $"CMDR:{(int)MessageKind.StartBuildingUpgrade}|{data}";
            return r;
        }
        private Response ProcessStartBuilding(PlayerData pd, ref RpgMessage msg)
        {
            var cmdData = msg.Data.Split("|");
            if (!int.TryParse(cmdData[0], out int slot))
            {
                return CreateErrorResponse(ref msg, "StartBuilding: Invalid slot");
            }
            if (!int.TryParse(cmdData[1], out int buildingId))
            {
                return CreateErrorResponse(ref msg, "StartBuilding: Invalid building id");
            }
            if (pd.City.buildings[slot] != null)
            {
                return CreateErrorResponse(ref msg, "StartBuilding: slot not empty");
            }

            //---------------------------
            // check resources
            //---------------------------
            var building = DataFactory.GetBuilding(buildingId);
            var isOk = building.UpgradeCost.food <= pd.City.resources.food &&
                       building.UpgradeCost.wood <= pd.City.resources.wood &&
                       building.UpgradeCost.stone <= pd.City.resources.stone;

            
            if (!isOk)
            {
                return CreateErrorResponse(ref msg, "StartBuilding: no resources");
            }
            else
            {
                //---------------------------
                //  check builders
                //---------------------------
                if(!UseBuilder(pd, building, slot))
                    return CreateErrorResponse(ref msg, "StartBuilding: no builder");
            }

            pd.City.resources -= building.UpgradeCost;
            pd.City.buildings[slot] = building;
            building.BuildTimeLeft = building.UpgradeTime;
            var data = JsonConvert.SerializeObject(new
            {
                slot,
                building
            });
            var r = CreateResponse(ref msg);
            r.Data = $"CMDR:{(int)MessageKind.StartBuilding}|{data}";
            return r;
        }

        /// <summary>
        /// Uses first available builder that can finish the building.
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="building"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        private bool UseBuilder(PlayerData pd, Building building, int slot)
        {
            bool hasBuilder = false;
            for (int i = 0; i < pd.City.builders.Length; i++)
            {
                if (pd.City.builders[i].buildTimeLeft <= 0 &&
                   pd.City.builders[i].expires >= DateTime.Now.AddMilliseconds(building.UpgradeTime))
                {
                    hasBuilder = true;
                    pd.City.builders[i].slot = slot;
                    pd.City.builders[i].buildTimeLeft = building.UpgradeTime;
                    break;
                }
            }
            return hasBuilder;
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
        /// Verifies that the ClientMessage.Kind is known and copies the ClientMessage into a new RpgMessage instance.
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
            switch (cm.Kind)
            {
                case MessageKind.StartBuilding:
                    break;
                case MessageKind.StartBuildingUpgrade:
                    break;
                case MessageKind.StartBuildingDestroy:
                    break;
                case MessageKind.Chat:
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
