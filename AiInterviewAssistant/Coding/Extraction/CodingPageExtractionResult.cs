using AiInterviewAssistant.Coding.Models;

namespace AiInterviewAssistant.Coding.Extraction
{
    public sealed class CodingPageExtractionResult
    {
        public CodingProblem Problem { get; set; }

        public bool UiAutomationSucceeded { get; set; }

        public bool RequiresVisionFallback { get; set; }

        public string FailureReason { get; set; }
    }
}