using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace PingKeeper.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class PingController : ControllerBase
    {

        [HttpGet(Name = "Ping")]
        public async Task<object> Get()
        {
            return "OK";
        }
    }
}