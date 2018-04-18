namespace ws_hero.Messages
{
    public struct RpgMessage
    {
        public string PlayerId { get; set; }

        public int Cid { get; set; }
        public long ClientTime { get; set; }

        public RpgType RpgType { get; set; }
        public string Data { get; set; }
    }
}