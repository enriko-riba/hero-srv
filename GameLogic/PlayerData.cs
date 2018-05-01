using Newtonsoft.Json;

namespace ws_hero.GameLogic
{
    public class PlayerData 
    {
        public PlayerData()
        {
            City = new City();
            CharData = new CharData();
        }
        public int coins { get; set; }
        public int gold { get; set; }
        public int exp { get; set; }

        [JsonProperty("city")]
        public City City { get; set; }

        [JsonProperty("charData")]
        public CharData CharData { get; set; }

        /// <summary>
        /// The place the players hero is currently located.
        /// </summary>
        [JsonProperty("currentPlaceId")]
        public int CurrentPlaceId { get; set; }

        /// <summary>
        /// The kingdom the players hero belongs to.
        /// </summary>
        [JsonProperty("kingdomId")]
        public int KingdomId { get; set; }
    }
}
