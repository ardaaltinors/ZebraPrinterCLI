using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        // Cache the discovery result.
        private (List<DiscoveredUsbPrinter> UsbPrinters, List<DiscoveredPrinter> NetworkPrinters)? _cachedPrinters;
        private readonly object _lock = new();

        public PrinterDiscoveryService(PrinterConfig config)
        {
            _config = config;
            _networkDiscoveryHandler = new NetworkDiscoveryHandler();
        }

        public async Task<(List<DiscoveredUsbPrinter> UsbPrinters, List<DiscoveredPrinter> NetworkPrinters)> DiscoverPrintersAsync()
        {
            // If we've already discovered printers, return the cached result.
            if (_cachedPrinters != null)
            {
                return _cachedPrinters.Value;
            }

            var usbPrinters = new List<DiscoveredUsbPrinter>();
            var networkPrinters = new List<DiscoveredPrinter>();

            try
            {
                if (_config.EnableUsbDiscovery)
                {
                    Console.WriteLine("Searching for USB printers...");
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

                if (_config.EnableNetworkDiscovery)
                {
                    Console.WriteLine("\nSearching for network printers...");
                    await Task.Run(() =>
                    {
                        NetworkCardDiscoverer.FindPrinters(_networkDiscoveryHandler);
                        _networkDiscoveryHandler.DiscoveryCompleteEvent.WaitOne();
                    });
                    networkPrinters = _networkDiscoveryHandler.DiscoveredPrinters;
                    Console.WriteLine($"Discovered {networkPrinters.Count} network printers.");
                }

                // Cache the result only if at least one printer is found.
                if (usbPrinters.Count > 0 || networkPrinters.Count > 0)
                {
                    lock (_lock)
                    {
                        if (_cachedPrinters == null)
                        {
                            _cachedPrinters = (usbPrinters, networkPrinters);
                        }
                    }
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
