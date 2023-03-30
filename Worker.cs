using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerServiceFakeApi
{
    public class Worker : BackgroundService
    {
        const int ThreadDelay = 5000; //ms

        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _config = configuration;
            _httpClient = httpClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stopToken)
        {
            string url = _config["Environment:BaseURL"];
            string filePath = _config["Environment:FilePath"];

            while (!stopToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                try
                {
                    var response = await _httpClient.GetAsync(url, stopToken);

                    if (response.IsSuccessStatusCode) // statusCode == 200/201/202
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using (StreamWriter sw = File.AppendText(filePath))
                        {
                            await sw.WriteLineAsync(json);
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    _logger.LogCritical($"{nameof(HttpRequestException)}: {e.Message}");
                }
                await Task.Delay(ThreadDelay, stopToken);
            }
        }
    }
}
