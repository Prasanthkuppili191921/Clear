namespace AiInterviewAssistant.Coding.AI
{
    public sealed class CodingAiRequest
    {
        public string Question { get; set; }

        public string Input { get; set; }

        public string Output { get; set; }

        public string Constraints { get; set; }

        public string Examples { get; set; }

        public string StarterCode { get; set; }

        public string Language { get; set; }
    }
}