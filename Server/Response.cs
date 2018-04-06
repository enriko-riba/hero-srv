namespace my_hero.Server
{
    public struct Response
    {
        public ulong Tick { get; set; }
        public int Cid { get; set; }
        public string Data { get; set; }

        public TargetKind TargetKind { get; set; }
        public string[] Targets { get; set; }

    }
    public enum TargetKind
    {
        All,
        TargetList,
        TargetAllExcept
    }
}