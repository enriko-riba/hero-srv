namespace ws_hero.GameLogic
{
    public class City
    {
        public City()
        {
            this.resources = new Resources();
            this.production = new Resources()
            {
                wood = 1f,
                food = 1f,
                stone = 1f
            };

            this.buildings = new Building[10];
        }

        /// <summary>
        /// Available resources.
        /// </summary>
        public Resources resources { get; set; }

        /// <summary>
        /// Bbase production in units/level
        /// </summary>
        public Resources production { get; set; }

        public Building[] buildings { get; set; }
    }
}
