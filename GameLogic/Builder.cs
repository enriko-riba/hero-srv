namespace ws_hero.GameLogic
{
    public class Builder
    {
        /// <summary>
        /// Time when the builder expires.
        /// </summary>
        public System.DateTime expires { get; set; }

        /// <summary>
        /// Current building remaining build time
        /// </summary>
        public int buildTimeLeft { get; set; }

        /// <summary>
        /// City building slot of current building.
        /// </summary>
        public int slot { get; set; }
    }
}
