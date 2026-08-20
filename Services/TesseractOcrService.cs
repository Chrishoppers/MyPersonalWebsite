using System.Diagnostics;
using System.Text;

namespace MyPersonalWebsite.Services
{
    public class TesseractOcrService : IOcrService
    {
        private readonly string _tesseractCmd;
        private readonly string _lang;

        public TesseractOcrService()
        {
            _tesseractCmd = Environment.GetEnvironmentVariable("TESSERACT_PATH") ?? "tesseract";
            _lang = Environment.GetEnvironmentVariable("TESSERACT_LANG") ?? "chi_sim+eng";
        }

        public async Task<string> RecognizeAsync(string imagePath)
        {
            try
            {
                // Use tesseract CLI to output to stdout
                var psi = new ProcessStartInfo
                {
                    FileName = _tesseractCmd,
                    Arguments = $"\"{imagePath}\" stdout -l {_lang}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return string.Empty;
                var sb = new StringBuilder();
                while (!proc.StandardOutput.EndOfStream)
                {
                    sb.AppendLine(await proc.StandardOutput.ReadLineAsync());
                }
                var err = await proc.StandardError.ReadToEndAsync();
                proc.WaitForExit(5000);
                if (!string.IsNullOrEmpty(err))
                {
                    Console.WriteLine($"Tesseract stderr: {err}");
                }
                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tesseract OCR failed: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
