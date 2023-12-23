using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PingKeeper.Services
{
    public interface IPingService
    {
        public Task PingInterval(string intervalCron);
    }
}
