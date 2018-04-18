namespace ws_hero.Messages
{
    public struct RpgMessage
    {
        public string PlayerId { get; set; }

        public int Cid { get; set; }

        public long ClientTime { get; set; }

        public MessageKind Kind { get; set; }

        public string Data { get; set; }

        public int Tag1 { get; set; }

        public int Tag2 { get; set; }

        public int[] ATag { get; set; }
    }
}