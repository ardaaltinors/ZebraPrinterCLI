using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zebra.Sdk.Card.Containers;
using Zebra.Sdk.Card.Job.Template;
using Zebra.Sdk.Card.Printer;
using Zebra.Sdk.Comm;
using ZebraPrinterCLI.Config;

namespace ZebraPrinterCLI.Services
{
    public class PrinterTemplateService
    {
        private readonly PrinterConfig _config;
        private const int CARD_FEED_TIMEOUT = 30000;

        public PrinterTemplateService(PrinterConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<int> PrintTemplateAsync(string printerConnectionString, string templateData, Dictionary<string, string> fieldData, int copies = 1)
        {
            ArgumentNullException.ThrowIfNull(printerConnectionString);
            ArgumentNullException.ThrowIfNull(templateData);
            ArgumentNullException.ThrowIfNull(fieldData);

            Connection? connection = null;
            ZebraCardPrinter? zebraCardPrinter = null;

            try
            {
                // Create connection to the printer
                connection = new UsbConnection(printerConnectionString);
                connection.Open();

                // Initialize printer
                zebraCardPrinter = ZebraCardPrinterFactory.GetInstance(connection);
                var zebraCardTemplate = new ZebraCardTemplate(zebraCardPrinter);

                // Get template fields and validate data
                List<string> templateFields = zebraCardTemplate.GetTemplateDataFields(templateData);
                ValidateFieldData(templateFields, fieldData);

                // Generate and send template job
                TemplateJob templateJob = zebraCardTemplate.GenerateTemplateDataJob(templateData, fieldData);
                int jobId = zebraCardPrinter.PrintTemplate(copies, templateJob);

                // Poll job status
                var jobStatus = await PollJobStatusAsync(jobId, zebraCardPrinter);
                Console.WriteLine($"Job {jobId} completed with status '{jobStatus.PrintStatus}'.");

                return jobId;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error printing template: {ex.Message}", ex);
            }
            finally
            {
                if (connection is not null || zebraCardPrinter is not null)
                {
                    CloseConnection(connection, zebraCardPrinter);
                }
            }
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

            while (true)
            {
                jobStatusInfo = zebraCardPrinter.GetJobStatus(jobId);

                if (!isFeeding)
                {
                    start = Math.Abs(Environment.TickCount);
                }

                isFeeding = jobStatusInfo.CardPosition.Contains("feeding");

                string alarmDesc = jobStatusInfo.AlarmInfo.Value > 0 ? $" ({jobStatusInfo.AlarmInfo.Description})" : "";
                string errorDesc = jobStatusInfo.ErrorInfo.Value > 0 ? $" ({jobStatusInfo.ErrorInfo.Description})" : "";

                Console.WriteLine($"Job {jobId}: status:{jobStatusInfo.PrintStatus}, position:{jobStatusInfo.CardPosition}, " +
                                $"alarm:{jobStatusInfo.AlarmInfo.Value}{alarmDesc}, error:{jobStatusInfo.ErrorInfo.Value}{errorDesc}");

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
                }
                else if (jobStatusInfo.PrintStatus.Contains("in_progress") && isFeeding)
                {
                    if (Math.Abs(Environment.TickCount) > start + CARD_FEED_TIMEOUT)
                    {
                        Console.WriteLine("The job timed out waiting for a card and was cancelled.");
                        zebraCardPrinter.Cancel(jobId);
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