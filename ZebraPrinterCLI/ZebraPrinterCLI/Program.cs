﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;
using ZebraPrinterCLI.Config;
using ZebraPrinterCLI.Services;

// Web API Setup and Configuration
var builder = WebApplication.CreateBuilder(args);

// Force Development environment
builder.Environment.EnvironmentName = "Development";

// Configure specific URLs
builder.WebHost.UseUrls("https://0.0.0.0:53039", "http://0.0.0.0:53040");

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure printer services
builder.Services.Configure<PrinterConfig>(builder.Configuration.GetSection("PrinterConfig"));
builder.Services.AddSingleton<PrinterConfig>(sp => sp.GetRequiredService<IOptions<PrinterConfig>>().Value);
builder.Services.AddSingleton<PrinterDiscoveryService>();
builder.Services.AddSingleton<PrinterTemplateService>();

var app = builder.Build();

// Always enable Swagger regardless of environment
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zebra Printer API V1");
    c.RoutePrefix = string.Empty; // serve the Swagger UI at the root URL
});

app.UseHttpsRedirection();

// Debug endpoint to list all available printers
app.MapGet("/printers", async (PrinterDiscoveryService discoveryService) =>
{
    try
    {
        var (usbPrinters, networkPrinters) = await discoveryService.DiscoverPrintersAsync();
        return Results.Ok(new
        {
            UsbPrinters = usbPrinters,
            NetworkPrinters = networkPrinters
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("GetPrinters")
.WithOpenApi();

// Print endpoint that automatically finds and uses available printer
app.MapPost("/print", async (PrinterDiscoveryService discoveryService, PrinterTemplateService templateService, [FromBody] PrintRequest request) =>
{
    try
    {
        // Find available printers
        var (usbPrinters, networkPrinters) = await discoveryService.DiscoverPrintersAsync();
        
        // Get the first available printer (prioritize USB over network)
        var printer = usbPrinters.FirstOrDefault() ?? networkPrinters.FirstOrDefault();
        
        if (printer == null)
        {
            return Results.Problem("No printers found. Please connect a printer and try again.");
        }

        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "template.xml");
        if (!File.Exists(templatePath))
        {
            return Results.NotFound("Template file not found");
        }

        string templateData = await File.ReadAllTextAsync(templatePath);
        
        try 
        {
            var (jobId, status) = await templateService.PrintTemplateAsync(printer, templateData, request.FieldData);
            
            // Check for printer errors or alarms
            if (status.AlarmInfo.Value > 0 || status.ErrorInfo.Value > 0)
            {
                var errorDetails = new Dictionary<string, object>
                {
                    ["JobId"] = jobId,
                    ["PrinterUsed"] = printer.ToString(),
                    ["PrinterType"] = printer is DiscoveredUsbPrinter ? "USB" : "Network",
                    ["Status"] = status.PrintStatus,
                    ["Position"] = status.CardPosition
                };

                if (status.AlarmInfo.Value > 0)
                {
                    errorDetails["AlarmCode"] = status.AlarmInfo.Value;
                    errorDetails["Message"] = status.AlarmInfo.Description;
                }

                if (status.ErrorInfo.Value > 0)
                {
                    errorDetails["ErrorCode"] = status.ErrorInfo.Value;
                    errorDetails["ErrorMessage"] = status.ErrorInfo.Description;
                }

                return Results.UnprocessableEntity(errorDetails);
            }

            return Results.Ok(new
            {
                JobId = jobId,
                PrinterUsed = printer.ToString(),
                PrinterType = printer is DiscoveredUsbPrinter ? "USB" : "Network",
                Status = status.PrintStatus,
                Position = status.CardPosition
            });
        }
        catch (TimeoutException tex)
        {
            return Results.UnprocessableEntity(new Dictionary<string, object>
            {
                ["Error"] = "Timeout",
                ["Message"] = tex.Message
            });
        }
        catch (Exception printEx)
        {
            // Create a dictionary to hold error details
            var errorDetails = new Dictionary<string, object>();

            // Check for specific printer status/alarm codes using regular expressions
            var statusMatch = Regex.Match(printEx.Message, @"status:(\w+)");
            var positionMatch = Regex.Match(printEx.Message, @"position:(\w+)");
            var alarmMatch = Regex.Match(printEx.Message, @"alarm:(\d+)");
            var errorMatch = Regex.Match(printEx.Message, @"error:(\d+)");

            // Extract error information if matches are found
            if (statusMatch.Success)
            {
                errorDetails["Status"] = statusMatch.Groups[1].Value;
            }
            if (positionMatch.Success)
            {
                errorDetails["Position"] = positionMatch.Groups[1].Value;
            }
            if (alarmMatch.Success)
            {
                int alarmCode = int.Parse(alarmMatch.Groups[1].Value);
                errorDetails["AlarmCode"] = alarmCode;
                errorDetails["Message"] = PrinterErrorHelper.GetPrinterErrorMessage(alarmCode);
            }
            if (errorMatch.Success)
            {
                errorDetails["ErrorCode"] = int.Parse(errorMatch.Groups[1].Value);
            }

            // If no specific error details were extracted, provide a generic message
            if (errorDetails.Count == 0)
            {
                errorDetails["Message"] = "An unexpected printer error occurred.";
            }

            // Include the original exception message for debugging purposes
            errorDetails["ExceptionMessage"] = printEx.Message;

            // Return a structured error response
            return Results.UnprocessableEntity(errorDetails);
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("Print")
.WithOpenApi();

await app.RunAsync();

// Request Models and Helper Methods
public record PrintRequest(Dictionary<string, string> FieldData);

static class PrinterErrorHelper
{
    public static string GetPrinterErrorMessage(int alarmCode)
    {
        return alarmCode switch
        {
            4001 => "Media: Out of cards. Please load cards into the printer.",
            4002 => "Media: Card jam. Please check and clear the card path.",
            4003 => "Ribbon: Out of ribbon. Please replace the ribbon.",
            4004 => "Ribbon: Ribbon jam. Please check and clear the ribbon path.",
            4005 => "Printer: Cover open. Please close the printer cover.",
            4006 => "Printer: Temperature error. Please wait for the printer to cool down.",
            4007 => "Printer: Communication error. Please check the connection.",
            _ => $"Printer error: Alarm code {alarmCode}"
        };
    }
}