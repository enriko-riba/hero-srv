namespace ws_hero.Messages
{
    public class ClientMessage
    {
        public int Cid { get; set; }
        public ClientMessageKind Kind { get; set; }
        public string Data { get; set; }
    }
}
