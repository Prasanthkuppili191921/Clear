using System.Threading;
using System.Threading.Tasks;

namespace AiInterviewAssistant.Coding.Extraction
{
    public interface ICodingPageExtractor
    {
        Task<CodingPageExtractionResult> ExtractAsync(
            CancellationToken cancellationToken);
    }
}