using System.Collections.Generic;

namespace AiInterviewAssistant.Coding.Models
{
    public sealed class CodingProblem
    {
        public string Title { get; set; }

        public string ProblemStatement { get; set; }

        public string InputDescription { get; set; }

        public string OutputDescription { get; set; }

        public string Constraints { get; set; }

        public string StarterCode { get; set; }

        public string Language { get; set; }

        public string SourceApplication { get; set; }

        public List<CodingExample> Examples { get; set; } =
            new List<CodingExample>();
    }
}