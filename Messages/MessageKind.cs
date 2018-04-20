namespace ws_hero.Messages
{
    public enum MessageKind
    {
        Invalid,
        System,
        Command,
        StartBuilding = 10,
        StartBuildingUpgrade = 11,
        Chat = 1024
    }
}