namespace ws_hero.GameLogic
{
    public class City
    {
        public City()
        {
            this.production = new Resources();
            production.wood = 1f;
            production.food = 1f;
            production.stone = 1f;

            this.buildings = new Building[10];
        }

        public float wood { get; set; }
        public float food { get; set; }
        public float stone { get; set; }

        /// <summary>
        /// Bbase production in units/level
        /// </summary>
        public Resources production { get; set; }

        public Building[] buildings { get; set; }
    }
}
