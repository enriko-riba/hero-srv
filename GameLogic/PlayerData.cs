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
        public City City { get; set; }
        public CharData CharData { get; set; }
    }
}
