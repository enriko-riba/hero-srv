﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ws_hero.GameLogic
{
    public class Kingdom
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("places")]
        public Place[] Places { get; set; }
    }

    public class Place
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }
    }
}
