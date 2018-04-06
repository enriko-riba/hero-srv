namespace my_hero.Server
{
    public struct Message
    {
        public int PlayerId { get; set; }
        public int Cid { get; set; }
        
        public Command Command { get; set; }
        public string Data { get; set; }

        public static Message FromClientData(int playerId, ClientMessageKind kind, string messsage)
        {
            //  TODO: define data structure, parse cmd, parse payload
            
            Command command;
            if (kind == ClientMessageKind.Chat)
                command = Command.Chat;
            else
                command = Command.NullCommand;
                
            return new Message()
            {
                PlayerId = playerId,
                Command = command
            };
        }
    }
}