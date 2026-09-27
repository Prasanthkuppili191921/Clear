using AiInterviewAssistant.Coding.AI;
using AiInterviewAssistant.Coding.Detection;
using AiInterviewAssistant.Coding.Extraction;
using System.Threading;
using System.Threading.Tasks;

namespace AiInterviewAssistant.Coding
{
    public sealed class CodingModeService
    {
        private readonly CodingPageCoordinator coordinator;

        public CodingModeService(
            CodingPageCoordinator coordinator)
        {
            this.coordinator = coordinator;
        }

        public Task<CodingAiResponse> ProcessAsync(
            CancellationToken cancellationToken)
        {
            return coordinator.ProcessAsync(
                cancellationToken);
        }
    }
}