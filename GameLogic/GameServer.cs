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

            city.food += city.production.food * seconds;
            city.wood += city.production.wood * seconds;
            city.stone += city.production.stone * seconds;
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
    }
}
