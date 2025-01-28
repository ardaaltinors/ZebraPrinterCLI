namespace ZebraPrinterCLI.Config
{
    public class PrinterConfig
    {
        public bool EnableUsbDiscovery { get; set; } = true;
        public bool EnableNetworkDiscovery { get; set; } = true;
        public int UsbConnectionTimeoutMs { get; set; } = 10000;
        public int UsbOperationTimeoutMs { get; set; } = 30000;
        public bool EnableUsbRetries { get; set; } = true;
        public int MaxUsbRetries { get; set; } = 3;
        public int UsbRetryDelayMs { get; set; } = 2000;
    }
} 