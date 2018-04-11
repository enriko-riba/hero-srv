using Newtonsoft.Json;

namespace ws_hero.Messages
{
    public struct Response
    {
        public ulong Tick { get; set; }
        public int Cid { get; set; }
        public string Data { get; set; }

        public string v { get => "0.1"; }

        [JsonIgnore]
        public TargetKind TargetKind { get; set; }
        [JsonIgnore]
        public string[] Targets { get; set; }

    }
    public enum TargetKind
    {
        All,
        TargetList,
        TargetAllExcept
    }
}