using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace StockChecker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string[] _urls = new string[]
        {
            "https://www.newegg.com/Product/ComboDealDetails?ItemList=Combo.4190361",
            "https://www.newegg.com/Product/ComboDealDetails?ItemList=Combo.4190363",
            "https://www.newegg.com/Product/ComboDealDetails?ItemList=Combo.4190357",
            "https://www.newegg.com/Product/ComboDealDetails?ItemList=Combo.4190483",
            "https://www.newegg.com/Product/ComboDealDetails?ItemList=Combo.4191565",
            "https://www.newegg.com/Product/ComboDealDetails?ItemList=Combo.4190359"
        };

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected async Task<bool> CheckStock(string url)
        {
            var wc = new WebClient();

            // Add headers to impersonate a web browser. Some web sites 
            // will not respond correctly without these headers
            wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.66 Safari/537.36");
            wc.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            wc.Headers.Add("Accept-Language", "en-US,en;q=0.9");

            var data = await wc.DownloadStringTaskAsync(url);
            if(data.IndexOf("Add Combo to Cart") != -1)
            {
                return true;
            }

            return false;
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int index = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking stock for {stock}", _urls[index]);
                var inStock = await CheckStock(_urls[index]);
                if (inStock)
                {
                    _logger.LogInformation(_urls[index] + " IN STOCK!!!!");
                    OpenUrl(_urls[index]);
                }
                index = (index + 1) % _urls.Length;
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
