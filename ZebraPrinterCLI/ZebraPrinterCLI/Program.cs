using System;
using System.Collections.Generic;
using System.Threading;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;

namespace ZebraPrinterCLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting printer discovery...");

                // Search for USB printers
                Console.WriteLine("Searching for USB printers...");
                List<DiscoveredUsbPrinter> usbPrinters = UsbDiscoverer.GetZebraUsbPrinters();
                Console.WriteLine($"Discovered {usbPrinters.Count} USB printers.");
                foreach (DiscoveredUsbPrinter printer in usbPrinters)
                {
                    Console.WriteLine($"USB Printer: {printer}");
                }

                // Search for Network printers
                Console.WriteLine("\nSearching for network printers...");
                NetworkDiscoveryHandler discoveryHandler = new NetworkDiscoveryHandler();
                NetworkDiscoverer.FindPrinters(discoveryHandler);
                discoveryHandler.DiscoveryCompleteEvent.WaitOne();
                
                List<DiscoveredPrinter> networkPrinters = discoveryHandler.DiscoveredPrinters;
                Console.WriteLine($"Discovered {networkPrinters.Count} network printers.");
                foreach (DiscoveredPrinter printer in networkPrinters)
                {
                    Console.WriteLine($"Network Printer: {printer}");
                }

                // Show total count
                int totalPrinters = usbPrinters.Count + networkPrinters.Count;
                Console.WriteLine($"\nTotal printers found: {totalPrinters}");
            }
            catch (DiscoveryException e)
            {
                Console.WriteLine($"Error discovering printers: {e.Message}");
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
