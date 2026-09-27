using System.Threading;
using System.Threading.Tasks;

namespace AiInterviewAssistant.Coding.AI
{
    public interface ICodingAiService
    {
        Task<CodingAiResponse> GenerateAnswerAsync(
            CodingAiRequest request,
            CancellationToken cancellationToken);
    }
}