using Newtonsoft.Json;

namespace ws_hero.GameLogic
{
    public class CharData
    {
        public CharData()
        {
            Slots = new int[16];
            Equipped = new int[9];
        }

        [JsonProperty("slots")]
        public int[] Slots { get; set; }

        [JsonProperty("equipped")]
        public int[] Equipped { get; set; }

        public enum EquipmentSlot
        {
            Head,
            Neck,
            HandL,
            Body,
            HandR,
            RingL,
            Legs,
            RingR,
            Boots,
        }
    }
}
