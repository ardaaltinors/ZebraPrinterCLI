using System.Collections.Generic;
using Zebra.Sdk.Card.Printer.Discovery;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;
using ZebraPrinterCLI.Config;

namespace ZebraPrinterCLI.Services
{
    public class PrinterDiscoveryService
    {
        private readonly PrinterConfig _config;
        private readonly NetworkDiscoveryHandler _networkDiscoveryHandler;

        public PrinterDiscoveryService(PrinterConfig config)
        {
            _config = config;
            _networkDiscoveryHandler = new NetworkDiscoveryHandler();
        }

        public async Task<(List<DiscoveredUsbPrinter> UsbPrinters, List<DiscoveredPrinter> NetworkPrinters)> DiscoverPrintersAsync()
        {
            var usbPrinters = new List<DiscoveredUsbPrinter>();
            var networkPrinters = new List<DiscoveredPrinter>();

            try
            {
                if (_config.EnableUsbDiscovery)
                {
                    Console.WriteLine("Searching for USB printers...");
                    try
                    {
                        // Run USB discovery in a background thread since it might block
                        usbPrinters = await Task.Run(() => 
                        {
                            Console.WriteLine("Starting USB discovery process...");
                            var printers = UsbDiscoverer.GetZebraUsbPrinters();
                            foreach (var printer in printers)
                            {
                                Console.WriteLine($"Found USB printer: {printer}, Address: {printer.Address}");
                            }
                            return printers;
                        });
                        Console.WriteLine($"Discovered {usbPrinters.Count} USB printers.");
                    }
                    catch (Exception usbEx)
                    {
                        Console.WriteLine($"Error during USB printer discovery: {usbEx.Message}");
                        throw;
                    }
                }

                if (_config.EnableNetworkDiscovery)
                {
                    Console.WriteLine("\nSearching for network printers...");
                    await Task.Run(() =>
                    {
                        NetworkCardDiscoverer.FindPrinters(_networkDiscoveryHandler);
                        _networkDiscoveryHandler.DiscoveryCompleteEvent.WaitOne();

                        List<DiscoveredPrinter> discoveredPrinters = _networkDiscoveryHandler.DiscoveredPrinters;

                       
                    });
                    networkPrinters = _networkDiscoveryHandler.DiscoveredPrinters;
                    Console.WriteLine($"Discovered {networkPrinters.Count} network printers.");
                }

                return (usbPrinters, networkPrinters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during printer discovery: {ex.Message}");
                throw;
            }
        }

        public void DisplayPrinters(List<DiscoveredUsbPrinter> usbPrinters, List<DiscoveredPrinter> networkPrinters)
        {
            if (_config.EnableUsbDiscovery)
            {
                foreach (var printer in usbPrinters)
                {
                    Console.WriteLine($"USB Printer: {printer}");
                }
            }

            if (_config.EnableNetworkDiscovery)
            {
                foreach (var printer in networkPrinters)
                {
                    Console.WriteLine($"Network Printer: {printer}");
                }
            }

            int totalPrinters = usbPrinters.Count + networkPrinters.Count;
            Console.WriteLine($"\nTotal printers found: {totalPrinters}");
        }
    }
} 