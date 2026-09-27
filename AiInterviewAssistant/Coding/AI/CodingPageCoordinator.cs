using AiInterviewAssistant.Coding.Detection;
using AiInterviewAssistant.Coding.Extraction;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiInterviewAssistant.Coding.AI
{
    public sealed class CodingPageCoordinator
    {
        private readonly ICodingPageDetector detector;
        private readonly ICodingPageExtractor extractor;
        private readonly ICodingAiService aiService;

        public CodingPageCoordinator(
            ICodingPageDetector detector,
            ICodingPageExtractor extractor,
            ICodingAiService aiService)
        {
            this.detector = detector
                ?? throw new ArgumentNullException(nameof(detector));

            this.extractor = extractor
                ?? throw new ArgumentNullException(nameof(extractor));

            this.aiService = aiService
                ?? throw new ArgumentNullException(nameof(aiService));
        }

        public async Task<CodingAiResponse> ProcessAsync(
            CancellationToken cancellationToken)
        {
            bool isCodingPage =
                await detector.IsCodingPageAsync(
                    cancellationToken);

            if (!isCodingPage)
            {
                return new CodingAiResponse
                {
                    Success = false,
                    ErrorMessage =
                        "Active page is not detected as a coding page."
                };
            }

            CodingPageExtractionResult extraction =
                await extractor.ExtractAsync(
                    cancellationToken);

            if (extraction == null ||
                extraction.Problem == null)
            {
                return new CodingAiResponse
                {
                    Success = false,
                    ErrorMessage =
                        extraction?.FailureReason ??
                        "Unable to extract coding question."
                };
            }

            var request =
                new CodingAiRequest
                {
                    Question =
                        extraction.Problem.ProblemStatement,

                    Input =
                        extraction.Problem.InputDescription,

                    Output =
                        extraction.Problem.OutputDescription,

                    Constraints =
                        extraction.Problem.Constraints,

                    StarterCode =
                        extraction.Problem.StarterCode,

                    Language =
                        extraction.Problem.Language,

                    Examples =
                        BuildExamples(
                            extraction.Problem)
                };

            return await aiService.GenerateAnswerAsync(
                request,
                cancellationToken);
        }

        private string BuildExamples(
            AiInterviewAssistant.Coding.Models.CodingProblem problem)
        {
            if (problem.Examples == null ||
                problem.Examples.Count == 0)
            {
                return string.Empty;
            }

            var builder =
                new System.Text.StringBuilder();

            for (
                int i = 0;
                i < problem.Examples.Count;
                i++)
            {
                var example =
                    problem.Examples[i];

                builder.AppendLine(
                    "Example " + (i + 1) + ":");

                if (!string.IsNullOrWhiteSpace(
                        example.Input))
                {
                    builder.AppendLine(
                        "Input: " +
                        example.Input);
                }

                if (!string.IsNullOrWhiteSpace(
                        example.Output))
                {
                    builder.AppendLine(
                        "Output: " +
                        example.Output);
                }

                if (!string.IsNullOrWhiteSpace(
                        example.Explanation))
                {
                    builder.AppendLine(
                        "Explanation: " +
                        example.Explanation);
                }

                builder.AppendLine();
            }

            return builder.ToString().Trim();
        }
    }
}