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

        public float wood { get; set; }
        public float food { get; set; }
        public float stone { get; set; }

        // base production
        public float prodWood { get; set; }
        public float prodFood { get; set; }
        public float prodStone { get; set; }
    }
}
