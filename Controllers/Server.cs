using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace my_hero.Controllers
{
    public class Server : Controller
    {
        [HttpGet("[action]")]
        public IActionResult Data()
        {
            var rng = new Random();
           return Ok(rng);
        }
    }
}
