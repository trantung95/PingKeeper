using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace PingKeeper.Services
{
    public class PingService : IPingService
    {
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IServiceProvider _serviceProvider;

        public PingService(
            IRecurringJobManager recurringJobManager
            , IServiceProvider serviceProvider
            )
        {
            _recurringJobManager = recurringJobManager;
            _serviceProvider = serviceProvider;
        }

        public async Task PingInterval(string intervalCron)
        {
            _recurringJobManager.AddOrUpdate("PingKepperInterval", () => PingThere(), intervalCron);

        }

        public async Task PingThere()
        {
            using (var serviceScope = _serviceProvider.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                var configuration = services.GetService<IConfiguration>();
                var pingKeeperOption = new PingKeeperOptions(configuration);

                foreach (var url in pingKeeperOption.Urls)
                {
                   await SendAsync(url, HttpMethod.Get);
                }
            }
        }


        private async Task SendAsync(string requestUri, HttpMethod httpMethod)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler { Credentials = new NetworkCredential() };
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                using (var httpClient = new HttpClient(handler))
                using (var request = new HttpRequestMessage(httpMethod, requestUri))
                {
                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        // Do nothing
                    }
                    else
                    {
                        // Log error
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: log to file and delete after some time
            }
        }
    }
}
