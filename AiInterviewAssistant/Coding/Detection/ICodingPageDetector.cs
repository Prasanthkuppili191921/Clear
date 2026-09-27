using System.Threading;
using System.Threading.Tasks;

namespace AiInterviewAssistant.Coding.Detection
{
    public interface ICodingPageDetector
    {
        Task<bool> IsCodingPageAsync(
            CancellationToken cancellationToken);
    }
}