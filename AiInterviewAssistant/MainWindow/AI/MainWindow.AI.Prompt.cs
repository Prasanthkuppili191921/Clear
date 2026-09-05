using AiInterviewAssistant.Settings.Resume;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {

        // =========================================================
        // ANSWER MODE
        // =========================================================

        private string BuildAnswerModeInstruction(
            string answerMode,
            string responseLength)
        {
            string mode =
                answerMode ?? "Normal";

            string length =
                responseLength ?? "Medium";


            if (mode.Equals(
                    "Short",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "Answer briefly. Keep the response suitable for approximately " +
                    "15 to 30 seconds of speaking.";
            }


            if (mode.Equals(
                    "Detailed",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "Give a detailed interview answer suitable for approximately " +
                    "60 to 120 seconds of speaking. " +
                    "Include a practical example when useful.";
            }


            if (length.Equals(
                    "Short",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "Give a concise interview answer. " +
                    "Keep it direct and easy to speak.";
            }


            if (length.Equals(
                    "Long",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "Give a detailed but practical interview answer. " +
                    "Include an example when useful.";
            }


            return
                "Give a balanced interview answer suitable for approximately " +
                "30 to 60 seconds of speaking.";
        }


        // =========================================================
        // SYSTEM PROMPT
        // =========================================================

        private string BuildSystemPrompt(
            AppSettings settings,
            string languageInstruction)
        {
            string customPrompt =
                settings == null
                    ? ""
                    : settings.SystemPrompt;


            string prompt;


            // =========================================================
            // DEFAULT SYSTEM PROMPT
            // =========================================================

            if (string.IsNullOrWhiteSpace(
                customPrompt))
            {
                prompt =
                    "You are a helpful software development assistant. " +

                    "Answer the user's current question accurately and clearly. " +

                    "Use the previous conversation only when it is relevant to the current question. " +

                    "Do not assume the user wants an interview-style answer unless explicitly instructed. " +

                    "For technical questions, provide the appropriate technical explanation, " +
                    "code, examples, or reasoning requested by the user. " +

                    "Keep the answer focused on the question.";
            }
            else
            {
                prompt =
                    customPrompt;
            }


            // =========================================================
            // LANGUAGE REQUIREMENT
            // =========================================================

            if (!string.IsNullOrWhiteSpace(
                    languageInstruction))
            {
                prompt +=
                    "\n\nLanguage requirement:\n" +
                    languageInstruction;
            }


            // =========================================================
            // RESUME CONTEXT
            // =========================================================

            string resumeText =
                settings == null
                    ? ""
                    : ResumeContextBuilder.Build(
                        settings.ResumeText);


            if (!string.IsNullOrWhiteSpace(
                resumeText))
            {
                prompt +=
                    "\n\n" +
                    "====================================================\n" +
                    "CANDIDATE RESUME CONTEXT\n" +
                    "====================================================\n\n" +

                    "Use the following resume as the primary source of truth " +
                    "for questions about the candidate's personal experience, " +
                    "projects, companies, roles, responsibilities, skills, " +
                    "technologies and achievements.\n\n" +

                    "Resume rules:\n" +

                    "- Answer experience-related questions in first person, " +
                    "as if the candidate is speaking directly to the interviewer.\n" +

                    "- When the resume contains a relevant project, technology " +
                    "or responsibility, use it as the practical example.\n" +

                    "- Do not invent companies, projects, responsibilities, " +
                    "technologies, achievements or experience that are not " +
                    "supported by the resume.\n" +

                    "- If the interviewer asks about something that is not " +
                    "mentioned in the resume, do not falsely claim that the " +
                    "candidate has that experience.\n" +

                    "- For general technical questions, answer normally using " +
                    "the candidate's known background where relevant.\n\n" +

                    "RESUME:\n" +
                    resumeText +

                    "\n\n" +
                    "====================================================\n" +
                    "END CANDIDATE RESUME CONTEXT\n" +
                    "====================================================";
            }


            // =========================================================
            // CODE FORMATTING REQUIREMENT
            // =========================================================

            prompt +=
                "\n\nCODE FORMATTING REQUIREMENT:\n" +

                "Whenever you provide SQL, C#, JavaScript, TypeScript, " +
                "HTML, CSS, JSON, XML, XAML, PowerShell or VB.NET code, " +
                "ALWAYS put the code inside a fenced Markdown code block " +
                "using the correct language identifier.\n\n" +

                "Examples:\n" +

                "```sql\n" +
                "SELECT * FROM Employee;\n" +
                "```\n\n" +

                "```csharp\n" +
                "var result = GetData();\n" +
                "```\n\n" +

                "```json\n" +
                "{ \"name\": \"John\" }\n" +
                "```\n\n" +

                "Do not place programming code as plain paragraphs when " +
                "a fenced code block can be used.\n" +

                "For SQL questions, put the SQL query in a fenced " +
                "```sql code block first, followed by any explanation.";


            // =========================================================
            // RETURN FINAL SYSTEM PROMPT
            // =========================================================

            return prompt;
        }

        // =========================================================
        // BUILD REQUEST MESSAGES
        // =========================================================

        private List<object> BuildMessages(
            AppSettings settings,
            string languageInstruction)
        {
            List<object> messages =
                new List<object>();


            // =========================================================
            // FIND CURRENT QUESTION
            // =========================================================

            string currentQuestion =
                string.Empty;

            int currentQuestionIndex =
                -1;


            if (conversationHistory != null)
            {
                for (int i = conversationHistory.Count - 1;
                     i >= 0;
                     i--)
                {
                    object item =
                        conversationHistory[i];


                    if (item == null)
                        continue;


                    try
                    {
                        dynamic message =
                            item;


                        string role =
                            message.role?.ToString();


                        if (string.Equals(
                            role,
                            "user",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            currentQuestion =
                                message.content?.ToString()
                                ?? string.Empty;


                            currentQuestionIndex =
                                i;


                            break;
                        }
                    }
                    catch
                    {
                    }
                }
            }


            // =========================================================
            // BUILD SYSTEM PROMPT
            //
            // IMPORTANT:
            // Smart ON and Smart OFF both use the SAME base
            // System Prompt, Resume Context and Code Formatting.
            //
            // Smart ON only adds SmartAnswerService.
            // Answer Mode is NOT added here.
            // =========================================================

            string systemPrompt =
                BuildSystemPrompt(
                    settings,
                    languageInstruction);


            // =========================================================
            // SMART ANSWER
            //
            // IMPORTANT:
            // SmartAnswerService is used ONLY when Smart Answer
            // is enabled.
            //
            // Answer Mode / Response Length are NOT added.
            // =========================================================

            if (_smartAnswerEnabled &&
                !string.IsNullOrWhiteSpace(currentQuestion))
            {
                systemPrompt +=
                    "\n\n" +
                    SmartAnswerService_existed.BuildInstruction(
                        currentQuestion);
            }


            // =========================================================
            // ADD SYSTEM MESSAGE
            // =========================================================

            messages.Add(
                new
                {
                    role = "system",
                    content = systemPrompt
                });


            // =========================================================
            // NO HISTORY
            // =========================================================

            if (conversationHistory == null ||
                conversationHistory.Count == 0)
            {
                return messages;
            }


            // =========================================================
            // NO CURRENT QUESTION
            // =========================================================

            if (currentQuestionIndex < 0)
            {
                return messages;
            }


            // =========================================================
            // FIND START OF LAST 4 QUESTIONS
            //
            // Current question = 4th question.
            // Include previous 3 questions.
            // =========================================================

            int startIndex =
                currentQuestionIndex;


            int previousQuestionsFound =
                0;


            for (int i = currentQuestionIndex - 1;
                 i >= 0;
                 i--)
            {
                object item =
                    conversationHistory[i];


                if (item == null)
                    continue;


                try
                {
                    dynamic message =
                        item;


                    string role =
                        message.role?.ToString();


                    if (string.Equals(
                        role,
                        "user",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        previousQuestionsFound++;


                        if (previousQuestionsFound == 3)
                        {
                            startIndex =
                                i;

                            break;
                        }
                    }
                }
                catch
                {
                }
            }


            // =========================================================
            // ADD LAST 4 QUESTIONS + ANSWERS
            // =========================================================

            for (int i = startIndex;
                 i <= currentQuestionIndex;
                 i++)
            {
                object historyMessage =
                    conversationHistory[i];


                if (historyMessage != null)
                {
                    messages.Add(
                        historyMessage);
                }
            }


            return messages;
        }
    }
}
