namespace ws_hero.GameLogic
{
    public class City
    {
        public City()
        {
            this.resources = new Resources();
            this.production = new Resources();
            this.buildings = new Building[20];            
        }
        public Builder[] builders { get; set; }
        /// <summary>
        /// Available resources.
        /// </summary>
        public Resources resources { get; set; }

        /// <summary>
        /// Total production in units/second.
        /// </summary>
        public Resources production { get; set; }

        public Building[] buildings { get; set; }

        /// <summary>
        /// Max resource storage.
        /// </summary>
        public int storageCap { get; private set; }

        internal void RecalculateProduction()
        {
            storageCap = 4000;  //  base cap

            const float baseFood = 0.5f;
            const float baseWood = 0.5f;
            const float baseStone = 0.2f;

            production.food = baseFood;
            production.wood = baseWood;
            production.stone = baseStone;
            foreach (var b in buildings)
            {
                if (b != null && b.Level > 0)
                {
                    production.food += b.Production.food * b.Level;
                    production.wood += b.Production.wood * b.Level;
                    production.stone += b.Production.stone * b.Level;

                    storageCap += b.Storage * b.Level;
                }
            }
        }
    }
}
