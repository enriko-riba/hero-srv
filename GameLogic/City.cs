using System;

namespace ws_hero.GameLogic
{
    public class City
    {
        public City()
        {
            this.resources = new Resources();
            this.production = new Resources();
            this.buildings = new Building[10];
        }

        /// <summary>
        /// Available resources.
        /// </summary>
        public Resources resources { get; set; }

        /// <summary>
        /// Base production in units/level
        /// </summary>
        public Resources production { get; set; }

        public Building[] buildings { get; set; }

        internal void RecalculateProduction()
        {
            const float baseFood = 1f;
            const float baseWood = 0.5f;
            const float baseStone = 0.2f;

            production.food = baseFood;
            production.wood = baseWood;
            production.stone = baseStone;
            foreach (var b in buildings)
            {
                if (b != null)
                {
                    production.food += b.Production.food * b.Level;
                    production.wood += b.Production.wood * b.Level;
                    production.stone += b.Production.stone * b.Level;
                }
            }
        }
    }
}
