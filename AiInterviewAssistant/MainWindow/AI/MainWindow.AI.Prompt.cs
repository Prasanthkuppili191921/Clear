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
            string prompt;

            // =========================================================
            // BASE SYSTEM PROMPT
            // =========================================================

            if (string.IsNullOrWhiteSpace(settings.SystemPrompt))
            {
                prompt =
                    "You are a helpful software development assistant. " +

                    "Answer the user's current question accurately, clearly, and directly. " +

                    // =================================================
                    // CONVERSATION CONTEXT
                    // =================================================

                    "The conversation history contains previous user questions and " +
                    "assistant answers. Treat this conversation history as an important " +
                    "source of context for understanding the current question. " +

                    "Before answering, determine whether the current question is " +
                    "standalone or whether it depends on the previous conversation. " +

                    "When the current question is a continuation of an earlier discussion, " +
                    "infer the relevant topic, subject, technology, problem, requirement, " +
                    "constraints, and intent from the conversation history and answer " +
                    "accordingly. " +

                    "Do not require the user to repeat information that is already available " +
                    "in the conversation history. " +

                    "A short or incomplete question may still be a continuation of the " +
                    "previous discussion. Interpret it using the most relevant coherent " +
                    "conversation context rather than treating it as a new unrelated request. " +

                    // =================================================
                    // CONTEXT SELECTION
                    // =================================================

                    "When multiple topics exist in the conversation history, determine " +
                    "which previous topic is semantically relevant to the current question. " +

                    "Prefer the most recent coherent discussion that logically connects " +
                    "to the current question. " +

                    "Do not select context merely because a word happens to match. " +
                    "Use the meaning, intent, subject, and relationship between the messages. " +

                    // =================================================
                    // CURRENT QUESTION PRIORITY
                    // =================================================

                    "The current question always has the highest priority. " +

                    "Previous conversation provides context for interpreting the current " +
                    "question, but must not override a clearly new request. " +

                    "If the user clearly starts a new topic, answer the new topic without " +
                    "incorrectly carrying unrelated context from an earlier discussion. " +

                    // =================================================
                    // FOLLOW-UP QUESTIONS
                    // =================================================

                    "If the current question depends on information discussed earlier, " +
                    "continue that discussion naturally and answer the intended follow-up. " +

                    "Do not assume that every short question starts a new topic. " +

                    "Determine the relationship between messages dynamically from their " +
                    "meaning and surrounding conversation. " +

                    "Do not rely on predefined follow-up phrases, keywords, fixed patterns, " +
                    "or hard-coded examples to determine whether a question is a follow-up. " +

                    // =================================================
                    // RESUME CONTEXT PRIORITY
                    // =================================================

                    "Candidate resume information may be provided as additional background " +
                    "context. However, resume information must not replace or override the " +
                    "active conversation context when the user is discussing a technical topic. " +

                    "Use the candidate resume when the current question is clearly about " +
                    "the candidate's experience, skills, projects, responsibilities, " +
                    "technologies, career background, or resume. " +

                    "Do not switch from an active technical conversation to resume information " +
                    "merely because the current question is short or ambiguous. " +

                    "When an ambiguous question can reasonably be interpreted as a continuation " +
                    "of the active technical discussion, prefer that technical conversation " +
                    "context over unrelated resume information. " +

                    // =================================================
                    // TECHNICAL ANSWERING
                    // =================================================

                    "For technical questions, provide the appropriate explanation, code, " +
                    "examples, reasoning, comparisons, troubleshooting steps, or " +
                    "implementation details requested by the user. " +

                    "When code is requested, provide practical, correct, and directly usable " +
                    "code that matches the technology and context being discussed. " +

                    "Keep the answer focused on the current request and avoid unnecessarily " +
                    "repeating information that has already been established.";
            }
            else
            {
                prompt = settings.SystemPrompt;

                // =====================================================
                // DYNAMIC CONVERSATION CONTEXT RULES
                // =====================================================

                prompt +=
                    "\n\n" +
                    "CONVERSATION CONTEXT RULES:\n" +

                    "The conversation history contains previous user questions and " +
                    "assistant answers. Use that history intelligently to understand " +
                    "the current question.\n\n" +

                    "Determine whether the current question is standalone or a " +
                    "continuation of an earlier discussion. If it is a continuation, " +
                    "infer the relevant topic, subject, technology, problem, requirement, " +
                    "constraints, and intent from the conversation history.\n\n" +

                    "Do not require the user to repeat information that is already " +
                    "available in the conversation history.\n\n" +

                    "A short or incomplete question may still depend on the previous " +
                    "conversation. Interpret it using the most relevant coherent context " +
                    "rather than automatically treating it as a new topic.\n\n" +

                    "When multiple topics exist, determine the relevant topic based on " +
                    "semantic meaning, intent, subject, and conversation continuity. " +
                    "Do not select context merely because of matching words.\n\n" +

                    "The current question always has the highest priority. Previous " +
                    "conversation should provide context but must not override a clearly " +
                    "new request.\n\n" +

                    "Do not rely on predefined follow-up phrases, keywords, fixed patterns, " +
                    "or hard-coded examples. Determine message relationships dynamically " +
                    "from their meaning and surrounding conversation.\n\n" +

                    "Candidate resume information is additional background context. " +
                    "Use it when the current question is clearly about the candidate's " +
                    "experience, skills, projects, responsibilities, technologies, career, " +
                    "or resume.\n\n" +

                    "Do not use unrelated resume information to answer a short or ambiguous " +
                    "question when there is an active technical discussion in the conversation. " +

                    "When an ambiguous question can reasonably be interpreted as a continuation " +
                    "of the active technical discussion, prefer that technical conversation " +
                    "context.\n\n" +

                    "For technical questions, provide the appropriate explanation, code, " +
                    "examples, reasoning, comparisons, troubleshooting steps, or " +
                    "implementation details requested by the user.";
            }

            // =========================================================
            // LANGUAGE INSTRUCTION
            // =========================================================

            if (!string.IsNullOrWhiteSpace(languageInstruction))
            {
                prompt +=
                    "\n\n" +
                    languageInstruction;
            }

            // =========================================================
            // RESUME CONTEXT
            // =========================================================

            if (!string.IsNullOrWhiteSpace(settings.ResumeText))
            {
                prompt +=
                    "\n\n" +
                    "CANDIDATE RESUME CONTEXT:\n" +
                    "The following information is the candidate's resume. " +
                    "Use this information when the current question is related to " +
                    "the candidate's experience, skills, projects, responsibilities, " +
                    "technologies, career background, or resume. " +
                    "Do not use resume information as the subject of an unrelated " +
                    "technical question.\n\n" +
                    settings.ResumeText;
            }

            // =========================================================
            // CODE FORMATTING
            // =========================================================

            prompt +=
                "\n\n" +
                "CODE FORMATTING RULES:\n" +
                "When providing code, always use proper Markdown fenced code blocks " +
                "with the appropriate programming language identifier. " +
                "Keep code readable, correctly formatted, and directly usable.";

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
            // FIND CURRENT USER QUESTION
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
                        // Ignore invalid history item
                    }
                }
            }

            // =========================================================
            // BUILD SYSTEM PROMPT
            // =========================================================

            string systemPrompt =
                BuildSystemPrompt(
                    settings,
                    languageInstruction);

            // =========================================================
            // SMART ANSWER
            //
            // Leave existing Smart Answer behavior untouched.
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
            // SYSTEM MESSAGE
            // =========================================================

            messages.Add(
                new
                {
                    role = "system",
                    content = systemPrompt
                });

            // =========================================================
            // NO CONVERSATION HISTORY
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
            // FIND LAST 3 COMPLETE CONVERSATION TURNS
            //
            // A turn is:
            //
            // USER
            // ASSISTANT
            //
            // We collect complete turns backwards so the current
            // conversation context is always preserved.
            // =========================================================

            List<int> turnStartIndexes =
                new List<int>();

            int assistantIndex =
                -1;

            for (int i = currentQuestionIndex - 1;
                 i >= 0;
                 i--)
            {
                object item =
                    conversationHistory[i];

                if (item == null)
                    continue;

                string role =
                    string.Empty;

                try
                {
                    dynamic message =
                        item;

                    role =
                        message.role?.ToString()
                        ?? string.Empty;
                }
                catch
                {
                    continue;
                }

                // -----------------------------------------------------
                // We found the assistant answer belonging to a
                // previous user question.
                // -----------------------------------------------------

                if (assistantIndex < 0 &&
                    string.Equals(
                        role,
                        "assistant",
                        StringComparison.OrdinalIgnoreCase))
                {
                    assistantIndex =
                        i;

                    continue;
                }

                // -----------------------------------------------------
                // Once an assistant answer was found, the next user
                // message before it is the start of that conversation
                // turn.
                // -----------------------------------------------------

                if (assistantIndex >= 0 &&
                    string.Equals(
                        role,
                        "user",
                        StringComparison.OrdinalIgnoreCase))
                {
                    turnStartIndexes.Add(i);

                    assistantIndex = -1;

                    if (turnStartIndexes.Count >= 3)
                    {
                        break;
                    }
                }
            }

            // =========================================================
            // DETERMINE HISTORY START
            // =========================================================

            int startIndex =
                currentQuestionIndex;

            if (turnStartIndexes.Count > 0)
            {
                startIndex =
                    turnStartIndexes[
                        turnStartIndexes.Count - 1];
            }

            // =========================================================
            // ADD PREVIOUS COMPLETE TURNS
            // =========================================================

            for (int i = startIndex;
                 i < currentQuestionIndex;
                 i++)
            {
                object historyMessage =
                    conversationHistory[i];

                if (historyMessage == null)
                    continue;

                try
                {
                    dynamic message =
                        historyMessage;

                    string role =
                        message.role?.ToString();

                    // Only actual user/assistant conversation
                    // messages are sent to the model.
                    if (string.Equals(
                            role,
                            "user",
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            role,
                            "assistant",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        messages.Add(
                            historyMessage);
                    }
                }
                catch
                {
                    // Ignore malformed history item
                }
            }

            // =========================================================
            // ALWAYS ADD CURRENT QUESTION LAST
            // =========================================================

            object currentMessage =
                conversationHistory[currentQuestionIndex];

            if (currentMessage != null)
            {
                messages.Add(
                    currentMessage);
            }

            // =========================================================
            // RETURN FINAL MESSAGE LIST
            // =========================================================

            return messages;
        }
    }
}
