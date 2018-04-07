namespace ws_hero.Messages
{
    public struct RpgMessage
    {
        public int PlayerId { get; set; }
        public int Cid { get; set; }
        
        public RpgType RpgType { get; set; }
        public string Data { get; set; }

        public static RpgMessage FromClientMessage(int playerId, ref ClientMessage cm)
        {
            //  TODO: define data structure, parse cmd, parse payload
            
            RpgType command;
            if (cm.Kind == ClientMessageKind.Chat)
                command = RpgType.Chat;
            else
                command = RpgType.NullCommand;
                
            return new RpgMessage()
            {
                PlayerId = playerId,
                RpgType = command,
                Cid = cm.Cid,
                Data = cm.Data
            };
        }
    }
}