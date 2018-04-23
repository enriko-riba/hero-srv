using Newtonsoft.Json;
using System;

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
        /// Time in milliseconds needed to complete bulding.
        /// </summary>
        [JsonProperty("buildTimeLeft")]
        public int BuildTimeLeft { get; set; }

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
        /// Basic building cost, used for upgrade cost calc.
        /// </summary>
        [JsonProperty("cost")]
        public Resources Cost { get; set; }

        /// <summary>
        /// The storage capacity of this building.
        /// </summary>
        [JsonProperty("storage")]
        public int Storage { get; set; }

        /// <summary>
        /// Returns build cost for next level.
        /// </summary>
        [JsonProperty("upgradeCost")]
        public Resources UpgradeCost { get => Cost * (Level + 1) * 4; }

        [JsonProperty("destroyRefund")]
        public Resources DestroyRefund { get => UpgradeCost / 4; }

        /// <summary>
        /// Returns the upgrade time in milliseconds.
        /// </summary>
        [JsonProperty("upgradeTime")]
        public int UpgradeTime { get => BuildTime * 1000 * (Level + 1) * 3; }
    }   
}
