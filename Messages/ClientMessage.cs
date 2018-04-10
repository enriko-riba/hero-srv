namespace ws_hero.Messages
{
    public struct ClientMessage
    {
        public int Created { get; set; }
        public int Cid { get; set; }
        public ClientMessageKind Kind { get; set; }
        public string Data { get; set; }
    }
}
