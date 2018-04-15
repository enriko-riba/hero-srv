using Newtonsoft.Json;

namespace ws_hero.GameLogic
{
    public class Item
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("desc")]
        public string Description { get; set; }

        public int dmg { get; set; }
        public int arm { get; set; }
    }
}
