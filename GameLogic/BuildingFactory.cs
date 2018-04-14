namespace ws_hero.GameLogic
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    public class BuildingFactory
    {
        static BuildingFactory()
        {
            Initialize();
        }

        #region static
        private static Dictionary<int, Building> buildingsList = new Dictionary<int, Building>();

        private static void Initialize()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var name = Path.Combine(path, "Buildings.json");
            var buildingTemplates = JsonConvert.DeserializeObject<Building[]>(File.ReadAllText(name));
            foreach (var bt in buildingTemplates)
                buildingsList.Add(bt.Id, bt);
        }

        public static Building CreateBuilding(int id)
        {
            var t = buildingsList[id];
            return new Building()
            {
                Id = id,
                Level = 1,
                Name = t.Name,
                Type = t.Type,
                BuildTime = t.BuildTime,
                Production = new Resources()
                {
                    food = t.Production.food,
                    wood = t.Production.wood,
                    stone = t.Production.stone
                },
                Cost = new Resources()
                {
                    food = t.Cost.food,
                    wood = t.Cost.wood,
                    stone = t.Cost.stone
                }
            };
        }
        #endregion
    }
}
