using System;

namespace ws_hero.Messages
{
    public struct RpgMessage
    {
        public string PlayerId { get; set; }

        public int Cid { get; set; }
        public int ClientTime { get; set; }

        public RpgType RpgType { get; set; }
        public string Data { get; set; }


        public static RpgMessage FromClientMessage(string playerId, ref ClientMessage cm)
        {
            //  TODO: define data structure, parse cmd, parse payload
            var msg = new RpgMessage()
            {
                PlayerId = playerId,
                Cid = cm.Cid,
                ClientTime = cm.Created,
                Data = cm.Data
            };

            switch (cm.Kind)
            {
                case ClientMessageKind.Chat:
                    msg.RpgType = RpgType.Chat;
                    break;

                case ClientMessageKind.Command:
                    ParseCommand(ref cm, ref msg);
                    break;

                default:
                    msg.RpgType = RpgType.NullCommand;
                    break;
            }

            return msg;
        }

        private static void ParseCommand(ref ClientMessage cm, ref RpgMessage msg)
        {
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
            }
            throw new Exception("INVALID CODE");
        }

    }
}