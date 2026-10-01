namespace AiInterviewAssistant.ScreenQuestion
{
    public sealed class UniversalScreenQuestionResult
    {
        public bool IsQuestion { get; set; }

        public bool IsSkillRelated { get; set; }

        public string Question { get; set; }

        public string QuestionType { get; set; }

        public string Options { get; set; }

        public string Reason { get; set; }
    }
}