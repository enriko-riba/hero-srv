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

        public int gold { get; set; }
        public int exp { get; set; }
        [JsonProperty("city")]
        public City City { get; set; }

        [JsonProperty("charData")]
        public CharData CharData { get; set; }
    }
}
