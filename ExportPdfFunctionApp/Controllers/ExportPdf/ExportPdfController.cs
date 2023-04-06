using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ExportPdfFunctionApp.Controllers.ExportPdf.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PuppeteerSharp;

namespace ExportPdfFunctionApp.Controllers.ExportPdf;

public static class ExportPdfController
{
    [FunctionName("ExportPdf")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "pdf")] HttpRequest req, ILogger log)
    {
        try
        {
            
            log.LogInformation("Start convert process");
            var memoryStream = new MemoryStream();
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<PdfRequest>(requestBody);
            log.LogInformation($"Request extracted with success {request.FileName}");
            
            var browserOptions = new BrowserFetcherOptions
            {
                Path = Path.GetTempPath(),
            };
            using var browserFetcher = new BrowserFetcher(browserOptions);
            
            log.LogInformation($"Start downloading Chromium");
            await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            log.LogInformation($"Finish downloading Chromium");
            
            var launchOptions = new LaunchOptions()
            {
                Headless = true,
                ExecutablePath = browserFetcher.RevisionInfo(BrowserFetcher.DefaultChromiumRevision).ExecutablePath,
            };
            
            log.LogInformation($"Start puppeteer");
            var browser = await Puppeteer.LaunchAsync(launchOptions);
            
            log.LogInformation($"Open browser to export");
            await using var page = await browser.NewPageAsync();
            
            log.LogInformation($"Set html template on content");
            await page.SetContentAsync(request.HtmlTemplate);

            var pdfOptions = new PdfOptions()
            {
                PrintBackground = true,
                MarginOptions = {
                    Top =  "16px"
                }
            };
            
            log.LogInformation($"Print pdf file");
            var pdfContent = await page.PdfStreamAsync(pdfOptions);
            pdfContent.Seek(0, SeekOrigin.Begin);
            
            log.LogInformation($"Copy to memoryStream");
            pdfContent.CopyTo(memoryStream);
            
            var file = memoryStream.ToArray();
            await browser.CloseAsync();
            log.LogInformation($"Close browser and finish process.");

            return new FileContentResult(file, "application/pdf"){
                FileDownloadName = $"{request.FileName}.pdf"
            };
        }
        catch (Exception ex)
        {
            log.LogError(ex.Message);
            return new BadRequestObjectResult(new
            {
                errors = new List<string>
                {
                    ex.Message
                }
            });
        }
    }
}