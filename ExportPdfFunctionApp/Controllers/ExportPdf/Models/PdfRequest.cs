using System.Text.Json.Serialization;

namespace ExportPdfFunctionApp.Controllers.ExportPdf.Models;

public class PdfRequest
{
    [JsonPropertyName("htmlTemplate")]
    public string HtmlTemplate { get; set; }
    
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }
}