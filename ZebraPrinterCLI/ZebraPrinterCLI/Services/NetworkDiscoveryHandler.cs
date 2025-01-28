using System.Collections.Generic;
using System.Threading;
using Zebra.Sdk.Printer.Discovery;

namespace ZebraPrinterCLI.Services
{
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