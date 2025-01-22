using System;
using System.Collections.Generic;
using System.Threading;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;
using ZebraPrinterCLI.Config;
using ZebraPrinterCLI.Services;
using Microsoft.Extensions.Configuration;

namespace ZebraPrinterCLI
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Build configuration
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Get printer configuration from appsettings.json
                var printerConfig = configuration.GetSection("PrinterConfig").Get<PrinterConfig>();
                
                if (printerConfig == null)
                {
                    throw new InvalidOperationException("Failed to load printer configuration from appsettings.json");
                }

                var discoveryService = new PrinterDiscoveryService(printerConfig);
                Console.WriteLine("Starting printer discovery...");

                var (usbPrinters, networkPrinters) = await discoveryService.DiscoverPrintersAsync();
                discoveryService.DisplayPrinters(usbPrinters, networkPrinters);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
            }
        }
    }

    internal class NetworkDiscoveryHandler : DiscoveryHandler
    {
        private List<DiscoveredPrinter> printers = new List<DiscoveredPrinter>();
        private AutoResetEvent discoCompleteEvent = new AutoResetEvent(false);

        public void DiscoveryError(string message)
        {
            Console.WriteLine($"An error occurred during discovery: {message}.");
            discoCompleteEvent.Set();
        }

        public void DiscoveryFinished()
        {
            discoCompleteEvent.Set();
        }

        public void FoundPrinter(DiscoveredPrinter printer)
        {
            printers.Add(printer);
        }

        public List<DiscoveredPrinter> DiscoveredPrinters
        {
            get => printers;
        }

        public AutoResetEvent DiscoveryCompleteEvent
        {
            get => discoCompleteEvent;
        }
    }
}
