using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingKeeper
{
    public class PingKeeperOptions
    {
        private readonly IConfiguration Configuration;

        public PingKeeperOptions(IConfiguration configuration)
        {
            Configuration = configuration;

            // Arrange values
            IntervalCron = Configuration["PingKeeper:IntervalCron"];
            var urlsSestion = Configuration.GetSection("PingKeeper:Urls").GetChildren();
            if (urlsSestion != null && urlsSestion.Any())
                Urls = urlsSestion.Select(x => x.Value).ToList();
        }

        public string IntervalCron { get; } = "* * * * *";
        public List<string> Urls { get; } = new List<string>();
    }
}
