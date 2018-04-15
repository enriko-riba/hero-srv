using Newtonsoft.Json;

namespace ws_hero.GameLogic
{
    public class Building
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("level")]
        public int Level { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public BuildingType Type { get; set; }

        /// <summary>
        /// Build time in seconds.
        /// </summary>
        [JsonProperty("buildTime")]
        public int BuildTime { get; set; }

        /// <summary>
        /// Increases base production in units/level
        /// </summary>
        [JsonProperty("production")]
        public Resources Production { get; set; }

        /// <summary>
        /// Level 1 cost in units
        /// </summary>
        [JsonProperty("cost")]
        public Resources Cost { get; set; }

        /// <summary>
        /// Returns build cost for next level.
        /// </summary>
        public Resources BuildCost { get => Cost * (Level + 1); }
    }   
}
