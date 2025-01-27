using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

                // Initialize services
                var discoveryService = new PrinterDiscoveryService(printerConfig);
                var templateService = new PrinterTemplateService(printerConfig);

                Console.WriteLine("Starting printer discovery...");
                var (usbPrinters, networkPrinters) = await discoveryService.DiscoverPrintersAsync();
                discoveryService.DisplayPrinters(usbPrinters, networkPrinters);

                if (usbPrinters.Count > 0 || networkPrinters.Count > 0)
                {
                    // For demo purposes, use the first available printer
                    var printer = usbPrinters.Count > 0 ? usbPrinters[0] : networkPrinters[0];
                    
                    // Example template data and field values
                    string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "template.xml");
                    if (!File.Exists(templatePath))
                    {
                        throw new FileNotFoundException($"Template file not found at: {templatePath}");
                    }
                    
                    string templateData = File.ReadAllText(templatePath);
                    var fieldData = new Dictionary<string, string>
                    {
                        { "serialNumber", "SN123456789" },
                        { "gemstone", "Diamond" },
                        { "material", "18K White Gold" },
                        { "totalCarat", "1.5" },
                        { "date", DateTime.Now.ToString("yyyy-MM-dd") },
                        //{ "qrCode", "123456789" }  // Numeric only value for testing
                    };

                    Console.WriteLine($"\nPrinting template to printer: {printer}");
                    int jobId = await templateService.PrintTemplateAsync(printer.ToString(), templateData, fieldData);
                    Console.WriteLine($"Print job completed with ID: {jobId}");
                }
                else
                {
                    Console.WriteLine("No printers found. Please connect a printer and try again.");
                }
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
