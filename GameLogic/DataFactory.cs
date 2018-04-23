namespace ws_hero.GameLogic
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    public class DataFactory
    {
        static DataFactory()
        {
            Initialize();
        }

        #region static
        private static Dictionary<int, Building> buildingsList = new Dictionary<int, Building>();
        private static Dictionary<int, Item> itemsList = new Dictionary<int, Item>();

        private static void Initialize()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var name = Path.Combine(path, "Buildings.json");
            var bTemp = JsonConvert.DeserializeObject<Building[]>(File.ReadAllText(name));
            foreach (var bt in bTemp)
                buildingsList.Add(bt.Id, bt);

            name = Path.Combine(path, "Items.json");
            var iTemp = JsonConvert.DeserializeObject<Item[]>(File.ReadAllText(name));
            foreach (var it in iTemp)
                itemsList.Add(it.Id, it);
        }

        public static IEnumerable<Item> GetItems() => itemsList.Values;

        public static IEnumerable<Building> GetBuildings() => buildingsList.Values;
       
        public static Building GetBuilding(int id)
        {
            var t = buildingsList[id];
            t.Production = t.Production ?? new Resources();
            return new Building()
            {
                Id = id,
                Level = 0,
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
                },
                Storage = t.Storage
            };
        }              
        #endregion
    }
}
