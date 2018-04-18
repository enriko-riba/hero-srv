namespace ws_hero.Messages
{
    public struct ClientMessage
    {
        public long Created { get; set; }
        public int Cid { get; set; }
        public MessageKind Kind { get; set; }
        public string Data { get; set; }
    }
}
