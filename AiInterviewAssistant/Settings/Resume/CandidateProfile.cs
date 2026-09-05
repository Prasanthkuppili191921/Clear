namespace AiInterviewAssistant.Settings.Resume
{
    public class CandidateProfile
    {
        public string Summary { get; set; } = "";
        public string Experience { get; set; } = "";
        public string Skills { get; set; } = "";
        public string Projects { get; set; } = "";
        public string Education { get; set; } = "";
        public string Certifications { get; set; } = "";

        public string BuildContext()
        {
            string context = "";

            if (!string.IsNullOrWhiteSpace(Summary))
                context += "SUMMARY:\n" + Summary + "\n\n";

            if (!string.IsNullOrWhiteSpace(Experience))
                context += "EXPERIENCE:\n" + Experience + "\n\n";

            if (!string.IsNullOrWhiteSpace(Skills))
                context += "SKILLS:\n" + Skills + "\n\n";

            if (!string.IsNullOrWhiteSpace(Projects))
                context += "PROJECTS:\n" + Projects + "\n\n";

            if (!string.IsNullOrWhiteSpace(Education))
                context += "EDUCATION:\n" + Education + "\n\n";

            if (!string.IsNullOrWhiteSpace(Certifications))
                context +=
                    "CERTIFICATIONS:\n" +
                    Certifications +
                    "\n\n";

            return context.Trim();
        }
    }
}