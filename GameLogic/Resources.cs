namespace ws_hero.GameLogic
{
    public class Resources
    {
        public float wood { get; set; }
        public float food { get; set; }
        public float stone { get; set; }

        public override string ToString() => $"F: {food}, W: {wood}, S: {stone}";

        public void Clamp(int cap)
        {
            if (food > cap) food = cap;
            if (wood > cap) wood = cap;
            if (stone > cap) stone = cap;
        }

        public void IncreaseWithClamp(Resources amount, int cap)
        {
            if (food < cap) food += amount.food;
            if (wood < cap) wood += amount.wood;
            if (stone < cap) stone += amount.stone;
        }
        public static Resources operator *(Resources res, int amount)
        {
            return new Resources()
            {
                wood = res.wood * amount,
                food = res.food * amount,
                stone = res.stone * amount
            };
        }

        public static Resources operator *(Resources res, float amount)
        {
            return new Resources()
            {
                wood = res.wood * amount,
                food = res.food * amount,
                stone = res.stone * amount
            };
        }

        public static Resources operator /(Resources res, int amount)
        {
            return new Resources()
            {
                wood = res.wood / amount,
                food = res.food / amount,
                stone = res.stone / amount
            };
        }

        public static Resources operator -(Resources res1, Resources res2)
        {
            return new Resources()
            {
                wood =  res1.wood - res2.wood ,
                food =  res1.food - res2.food ,
                stone = res1.stone -res2.stone
            };
        }

        public static Resources operator +(Resources res1, Resources res2)
        {
            return new Resources()
            {
                wood = res1.wood + res2.wood,
                food = res1.food + res2.food,
                stone = res1.stone + res2.stone
            };
        }
    }
}
