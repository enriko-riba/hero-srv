namespace ws_hero.DAL
{
    public class GameData
    {
        public GameData()
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
