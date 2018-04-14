namespace ws_hero.GameLogic
{
    public class Building
    {
        public int Id { get; set; }
        public int Level { get; set; }

        /// <summary>
        /// Increases base production in units/level
        /// </summary>
        public Production Production { get; set; }      
    }   
}
