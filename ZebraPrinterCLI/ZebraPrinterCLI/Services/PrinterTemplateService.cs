using System;
using System.Collections.Generic;
using System.IO; // Needed for FileNotFoundException
using System.Threading;
using System.Threading.Tasks;
using Zebra.Sdk.Card.Containers;
using Zebra.Sdk.Card.Job.Template;
using Zebra.Sdk.Card.Printer;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer.Discovery;
using ZebraPrinterCLI.Config;

namespace ZebraPrinterCLI.Services
{
    public class PrinterTemplateService
    {
        private readonly PrinterConfig _config;
        private const int CARD_FEED_TIMEOUT = 30000;
        private const int MAX_POLLING_TIME = 60000; // Maximum time to poll for job status (60 seconds)
        private const int MAX_RETRIES = 3;
        private const int RETRY_DELAY_MS = 2000;

        public PrinterTemplateService(PrinterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<(int JobId, JobStatusInfo Status)> PrintTemplateAsync(DiscoveredPrinter printerConnectionString, string templateData, Dictionary<string, string> fieldData, int copies = 1)
        {
            ArgumentNullException.ThrowIfNull(printerConnectionString);
            ArgumentNullException.ThrowIfNull(templateData);
            ArgumentNullException.ThrowIfNull(fieldData);

            Connection? connection = null;
            ZebraCardPrinter? zebraCardPrinter = null;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        Console.WriteLine($"Retry attempt {attempt} of {MAX_RETRIES}...");
                        await Task.Delay(RETRY_DELAY_MS);
                    }

                    // Create connection to the printer with error handling
                    try
                    {
                        Console.WriteLine($"Attempting to create USB connection to: {printerConnectionString}");
                        connection = printerConnectionString.GetConnection();
                        connection.Open();
                        
                        Console.WriteLine("USB connection created successfully");
                    }
                    catch (Exception connEx)
                    {
                        throw new Exception($"Failed to create USB connection: {connEx.Message}", connEx);
                    }

                    // Initialize printer with error handling
                    try
                    {
                        Console.WriteLine("Initializing printer...");
                        zebraCardPrinter = ZebraCardPrinterFactory.GetInstance(connection);
                        Console.WriteLine("Printer initialized successfully");
                    }
                    catch (Exception initEx)
                    {
                        throw new Exception($"Failed to initialize printer: {initEx.Message}", initEx);
                    }
                    
                    // Verify printer is ready
                    var printerStatus = zebraCardPrinter.GetPrinterStatus();
                    if (printerStatus.Status != "ready" && printerStatus.Status != "idle")
                    {
                        throw new InvalidOperationException($"Printer is not ready. Status: {printerStatus.Status}");
                    }

                    // Create template handler
                    ZebraCardTemplate zebraCardTemplate = new ZebraCardTemplate(zebraCardPrinter);
                    string templateName = "template";
                    
                    // Attempt to delete the existing template file; if it doesn't exist, log and continue.
                    try
                    {
                        zebraCardTemplate.DeleteTemplateFileData(templateName);
                    }
                    catch (FileNotFoundException)
                    {
                        Console.WriteLine("Template file not found; continuing with saving the new template.");
                    }

                    // Save the new template data
                    zebraCardTemplate.SaveTemplateFileData(templateName, templateData);

                    // Get template fields and validate data
                    List<string> templateFields = zebraCardTemplate.GetTemplateDataFields(templateData);
                    ValidateFieldData(templateFields, fieldData);

                    // Generate and send template job
                    TemplateJob templateJob = zebraCardTemplate.GenerateTemplateJob(templateName, fieldData);
                    int jobId = zebraCardPrinter.PrintTemplate(copies, templateJob);

                    // Poll job status
                    var jobStatus = await PollJobStatusAsync(jobId, zebraCardPrinter);
                    Console.WriteLine($"Job {jobId} completed with status '{jobStatus.PrintStatus}'.");

                    return (jobId, jobStatus);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                    
                    // Clean up resources before retry
                    CloseConnection(connection, zebraCardPrinter);
                    connection = null;
                    zebraCardPrinter = null;

                    if (attempt == MAX_RETRIES)
                    {
                        throw new Exception($"Error printing template after {MAX_RETRIES} attempts: {ex.Message}", ex);
                    }
                }
            }

            // This should never be reached due to the throw in the loop, but compiler doesn't know that
            throw new Exception($"Error printing template: {lastException?.Message}", lastException);
        }

        private void ValidateFieldData(List<string> templateFields, Dictionary<string, string> fieldData)
        {
            ArgumentNullException.ThrowIfNull(templateFields);
            ArgumentNullException.ThrowIfNull(fieldData);

            foreach (string field in templateFields)
            {
                if (!fieldData.ContainsKey(field) || string.IsNullOrEmpty(fieldData[field]))
                {
                    throw new ArgumentException($"Missing or empty value for template field: {field}");
                }
            }
        }

        private async Task<JobStatusInfo> PollJobStatusAsync(int jobId, ZebraCardPrinter zebraCardPrinter)
        {
            ArgumentNullException.ThrowIfNull(zebraCardPrinter);

            JobStatusInfo jobStatusInfo = new JobStatusInfo();
            bool isFeeding = false;
            long start = Math.Abs(Environment.TickCount);
            long pollStart = start;

            while (true)
            {
                // Check if we've exceeded the maximum polling time
                if (Math.Abs(Environment.TickCount) > pollStart + MAX_POLLING_TIME)
                {
                    throw new TimeoutException($"Job status polling timed out after {MAX_POLLING_TIME / 1000} seconds");
                }

                jobStatusInfo = zebraCardPrinter.GetJobStatus(jobId);

                if (!isFeeding)
                {
                    start = Math.Abs(Environment.TickCount);
                }

                isFeeding = jobStatusInfo.CardPosition.Contains("feeding");

                string alarmDesc = jobStatusInfo.AlarmInfo.Value > 0 ? $" ({jobStatusInfo.AlarmInfo.Description})" : "";
                string errorDesc = jobStatusInfo.ErrorInfo.Value > 0 ? $" ({jobStatusInfo.ErrorInfo.Description})" : "";

                string statusMessage = $"Job {jobId}: status:{jobStatusInfo.PrintStatus}, position:{jobStatusInfo.CardPosition}, " +
                                       $"alarm:{jobStatusInfo.AlarmInfo.Value}{alarmDesc}, error:{jobStatusInfo.ErrorInfo.Value}{errorDesc}";
                Console.WriteLine(statusMessage);

                // Check for immediate error conditions
                if (jobStatusInfo.AlarmInfo.Value > 0 || jobStatusInfo.ErrorInfo.Value > 0)
                {
                    return jobStatusInfo;
                }

                if (jobStatusInfo.PrintStatus.Contains("done_ok"))
                {
                    break;
                }
                else if (jobStatusInfo.PrintStatus.Contains("error") || jobStatusInfo.PrintStatus.Contains("cancelled"))
                {
                    Console.WriteLine($"The job encountered an error [{jobStatusInfo.ErrorInfo.Description}] and was cancelled.");
                    break;
                }
                else if (jobStatusInfo.ErrorInfo.Value > 0)
                {
                    Console.WriteLine($"The job encountered an error [{jobStatusInfo.ErrorInfo.Description}] and was cancelled.");
                    zebraCardPrinter.Cancel(jobId);
                    break;
                }
                else if (jobStatusInfo.PrintStatus.Contains("in_progress") && isFeeding)
                {
                    if (Math.Abs(Environment.TickCount) > start + CARD_FEED_TIMEOUT)
                    {
                        Console.WriteLine("The job timed out waiting for a card and was cancelled.");
                        zebraCardPrinter.Cancel(jobId);
                        break;
                    }
                }

                await Task.Delay(1000);
            }

            return jobStatusInfo;
        }

        private void CloseConnection(Connection? connection, ZebraCardPrinter? zebraCardPrinter)
        {
            try
            {
                if (zebraCardPrinter is not null)
                {
                    zebraCardPrinter.Destroy();
                }
            }
            catch { }

            try
            {
                if (connection is not null)
                {
                    connection.Close();
                }
            }
            catch { }
        }
    }
}
