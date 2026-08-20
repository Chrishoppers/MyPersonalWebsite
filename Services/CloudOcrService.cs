using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyPersonalWebsite.Services
{
    public class CloudOcrService : IOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _googleApiKey;
        private readonly string? _ocrSpaceKey;

        public CloudOcrService(IHttpClientFactory httpFactory)
        {
            _httpClient = httpFactory.CreateClient();
            _googleApiKey = Environment.GetEnvironmentVariable("GOOGLE_VISION_API_KEY");
            _ocrSpaceKey = Environment.GetEnvironmentVariable("OCR_SPACE_API_KEY");
        }

        public async Task<string> RecognizeAsync(string imagePath)
        {
            // Try Google Vision first if key present
            if (!string.IsNullOrEmpty(_googleApiKey))
            {
                try
                {
                    var bytes = await File.ReadAllBytesAsync(imagePath);
                    var base64 = Convert.ToBase64String(bytes);
                    var req = new
                    {
                        requests = new[]
                        {
                            new
                            {
                                image = new { content = base64 },
                                features = new[] { new { type = "DOCUMENT_TEXT_DETECTION" } }
                            }
                        }
                    };
                    var json = JsonSerializer.Serialize(req);
                    var resp = await _httpClient.PostAsync($"https://vision.googleapis.com/v1/images:annotate?key={_googleApiKey}",
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    var body = await resp.Content.ReadAsStringAsync();
                    if (resp.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;
                        var text = root.GetProperty("responses")[0].GetProperty("fullTextAnnotation").GetProperty("text").GetString();
                        return text ?? string.Empty;
                    }
                    Console.WriteLine($"Google Vision error: {body}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Google Vision request failed: {ex.Message}");
                }
            }

            // Fallback to OCR.space
            if (!string.IsNullOrEmpty(_ocrSpaceKey))
            {
                try
                {
                    var bytes = await File.ReadAllBytesAsync(imagePath);
                    var base64 = Convert.ToBase64String(bytes);
                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent($"data:image/png;base64,{base64}"), "base64Image");
                    content.Add(new StringContent("True"), "isOverlayRequired");
                    content.Add(new StringContent(_ocrSpaceKey), "apikey");

                    var resp = await _httpClient.PostAsync("https://api.ocr.space/parse/image", content);
                    var body = await resp.Content.ReadAsStringAsync();
                    if (resp.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;
                        var parsed = root.GetProperty("ParsedResults")[0];
                        var text = parsed.GetProperty("ParsedText").GetString();
                        return text ?? string.Empty;
                    }
                    Console.WriteLine($"OCR.space error: {body}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OCR.space request failed: {ex.Message}");
                }
            }

            return string.Empty;
        }
    }
}
