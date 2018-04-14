namespace ws_hero.GameLogic
{
    public class Building
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public BuildingType Type { get; set; }

        /// <summary>
        /// Build time in seconds.
        /// </summary>
        public int BuildTime { get; set; }

        /// <summary>
        /// Increases base production in units/level
        /// </summary>
        public Resources Production { get; set; }

        /// <summary>
        /// Level 1 cost in units
        /// </summary>
        public Resources Cost { get; set; }

        /// <summary>
        /// Returns build cost for next level.
        /// </summary>
        public Resources BuildCost { get => Cost * (Level + 1); }
    }   
}
