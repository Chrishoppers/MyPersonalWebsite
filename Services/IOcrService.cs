using System.Threading.Tasks;

namespace MyPersonalWebsite.Services
{
    public interface IOcrService
    {
        /// <summary>
        /// Recognize text from image file at given path and return plain text.
        /// </summary>
        Task<string> RecognizeAsync(string imagePath);
    }
}
