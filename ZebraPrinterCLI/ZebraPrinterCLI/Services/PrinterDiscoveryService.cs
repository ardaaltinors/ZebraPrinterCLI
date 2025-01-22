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
                    usbPrinters = UsbDiscoverer.GetZebraUsbPrinters();
                    Console.WriteLine($"Discovered {usbPrinters.Count} USB printers.");
                }

                if (_config.EnableNetworkDiscovery)
                {
                    Console.WriteLine("\nSearching for network printers...");
                    NetworkDiscoverer.FindPrinters(_networkDiscoveryHandler);
                    _networkDiscoveryHandler.DiscoveryCompleteEvent.WaitOne();
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