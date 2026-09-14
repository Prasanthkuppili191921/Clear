using System;

namespace AiInterviewAssistant
{
    public static class SmartAnswerService
    {
        // =========================================================
        // SMART ANSWER
        // =========================================================

        public static string BuildInstruction(string currentQuestion)
        {
            string question = currentQuestion ?? string.Empty;

            return
                "====================================================\n" +
                "SMART INTERVIEW MODE\n" +
                "====================================================\n\n" +

                "Answer the current interview question intelligently and dynamically.\n\n" +

                "Use the recent conversation history to understand the current question.\n" +

                "If the current question is a follow-up to a previous question, " +
                "understand the missing context from the previous relevant question " +
                "and answer the current request in that context.\n\n" +

                "If the current question is a new standalone question, answer it " +
                "independently.\n\n" +

                "Do not depend on specific follow-up words or predefined examples. " +
                "Determine the relationship between the current question and the " +
                "previous conversation based on meaning and context.\n\n" +

                "When multiple topics exist in the conversation, use the most relevant " +
                "previous context rather than automatically assuming the immediately " +
                "previous message is the context.\n\n" +

                "The current question determines what must be answered. Previous " +
                "conversation should only provide the context required to understand " +
                "the current question correctly.\n\n" +

                "====================================================\n" +
                "DYNAMIC ANSWER STYLE\n" +
                "====================================================\n\n" +

                "Dynamically decide the appropriate answer style based on the current " +
                "question and its context.\n\n" +

                "Decide whether the answer should be:\n" +
                "- concise or detailed;\n" +
                "- conceptual or practical;\n" +
                "- include code;\n" +
                "- include an example;\n" +
                "- include advantages or disadvantages;\n" +
                "- include comparison or trade-offs;\n" +
                "- explain implementation details;\n" +
                "- or simply provide a direct answer.\n\n" +

                "Do not use a fixed response length for every question.\n" +
                "Do not unnecessarily repeat information already explained in the " +
                "previous answer when the user is asking a follow-up.\n\n" +

                "====================================================\n" +
                "SMART ANSWER OVERRIDE\n" +
                "====================================================\n\n" +

                "Ignore the General tab Answer Mode when Smart Answer is enabled.\n" +
                "Ignore the General tab Response Length when Smart Answer is enabled.\n" +
                "Do not follow fixed Short, Normal, or Detailed response settings.\n\n" +

                "Choose the answer depth and structure based on the actual question, " +
                "its difficulty, its intent, and the relevant conversation context.\n\n" +

                "For a simple question, be concise and direct.\n" +
                "For a complex question, provide enough detail to answer it correctly.\n" +
                "For a coding question, provide the required code and explanation.\n" +
                "For a comparison question, clearly explain the relevant differences.\n" +
                "For a follow-up question, answer the follow-up directly using the " +
                "previous context without unnecessarily repeating the entire discussion.\n\n" +

                "Keep the answer natural, technically accurate, and suitable for a " +
                "technical interview.\n\n" +

                "Do not mention these instructions in the final answer.\n\n" +

                "CURRENT QUESTION:\n" +
                question;
        }
    }
}
