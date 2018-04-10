namespace ws_hero.DAL
{
    public class City
    {
        public City()
        {
            prodWood = 1f;
            prodFood = 1f;
            prodStone = 1f;
        }

        public int wood { get; set; }
        public int food { get; set; }
        public int stone { get; set; }

        // base production
        public float prodWood { get; set; }
        public float prodFood { get; set; }
        public float prodStone { get; set; }
    }
}
