namespace ws_hero.GameLogic
{
    public class Resources
    {
        public float wood { get; set; }
        public float food { get; set; }
        public float stone { get; set; }

        public static Resources operator *(Resources res, int amount)
        {
            return new Resources()
            {
                wood = res.wood * amount,
                food = res.food * amount,
                stone = res.stone * amount
            };
        }
    }
}
