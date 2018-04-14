namespace ws_hero.GameLogic
{
    public class BuildingTemplate
    {
        public int Id { get; set; }
        public BuildingType Type { get; set; }
        public string Name { get; set; }
        public Production Production { get; set; }
    }
}
