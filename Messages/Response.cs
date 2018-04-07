using Newtonsoft.Json;

namespace ws_hero.Messages
{
    public struct Response
    {
        public ulong Tick { get; set; }
        public int Cid { get; set; }
        public string Data { get; set; }

        [JsonIgnore]
        public TargetKind TargetKind { get; set; }
        [JsonIgnore]
        public int[] Targets { get; set; }

    }
    public enum TargetKind
    {
        All,
        TargetList,
        TargetAllExcept
    }
}