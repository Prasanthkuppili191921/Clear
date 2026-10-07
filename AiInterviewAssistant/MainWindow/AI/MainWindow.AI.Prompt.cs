using AiInterviewAssistant.Settings.Resume;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
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
                //return
                //    "Answer briefly. Keep the response suitable for approximately " +
                //    "15 to 30 seconds of speaking.";

                return
                   "Give a balanced, complete interview answer suitable for approximately " +
                   "30 to 60 seconds of speaking.";
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
                //return
                //    "Give a concise interview answer. " +
                //    "Keep it direct and easy to speak.";

                return
                    "Give a balanced, complete interview answer suitable for approximately " +
                    "30 to 60 seconds of speaking.";
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

            messages.Add(
                new
                {
                    role = "system",
                    content = systemPrompt
                });

            // =========================================================
            // NO HISTORY / NO CURRENT QUESTION
            // =========================================================

            if (conversationHistory == null ||
                conversationHistory.Count == 0 ||
                currentQuestionIndex < 0)
            {
                return messages;
            }

            // =========================================================
            // FIND IMMEDIATELY PREVIOUS COMPLETE TURN
            //
            // USER
            // ASSISTANT
            //
            // We only look at the immediately previous turn.
            // =========================================================

            int previousUserIndex =
                -1;

            int previousAssistantIndex =
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

                // ---------------------------------------------------------
                // First assistant found = previous answer
                // ---------------------------------------------------------

                if (previousAssistantIndex < 0 &&
                    string.Equals(
                        role,
                        "assistant",
                        StringComparison.OrdinalIgnoreCase))
                {
                    previousAssistantIndex =
                        i;

                    continue;
                }

                // ---------------------------------------------------------
                // User before that assistant = previous question
                // ---------------------------------------------------------

                if (previousAssistantIndex >= 0 &&
                    string.Equals(
                        role,
                        "user",
                        StringComparison.OrdinalIgnoreCase))
                {
                    previousUserIndex =
                        i;

                    break;
                }
            }

            // =========================================================
            // CONTEXT-AWARE HISTORY
            // =========================================================
            //
            // Only send the previous Q&A when the current question
            // appears to depend on the previous discussion.
            //
            // New standalone questions:
            //     Resume + Current Question
            //
            // Follow-up questions:
            //     Resume + Previous Q&A + Current Question
            // =========================================================

            bool usePreviousTurn =
                false;

            if (previousUserIndex >= 0 &&
                previousAssistantIndex >= 0)
            {
                string previousQuestion =
                    GetConversationMessageContent(
                        conversationHistory[previousUserIndex]);

                string previousAnswer =
                    GetConversationMessageContent(
                        conversationHistory[previousAssistantIndex]);

                usePreviousTurn =
                    IsLikelyFollowUpQuestion(
                        currentQuestion,
                        previousQuestion,
                        previousAnswer);
            }

            // =========================================================
            // ADD PREVIOUS TURN ONLY WHEN REQUIRED
            // =========================================================

            if (usePreviousTurn)
            {
                object previousUserMessage =
                    conversationHistory[previousUserIndex];

                object previousAssistantMessage =
                    conversationHistory[previousAssistantIndex];

                if (previousUserMessage != null)
                {
                    messages.Add(
                        previousUserMessage);
                }

                if (previousAssistantMessage != null)
                {
                    messages.Add(
                        previousAssistantMessage);
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

            return messages;
        }

        private string GetConversationMessageContent(
    object message)
        {
            if (message == null)
                return string.Empty;

            try
            {
                dynamic item =
                    message;

                return item.content?.ToString()
                       ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool IsLikelyFollowUpQuestion(
    string currentQuestion,
    string previousQuestion,
    string previousAnswer)
        {
            if (string.IsNullOrWhiteSpace(currentQuestion))
                return false;

            string current =
                currentQuestion.Trim();

            // =========================================================
            // VERY SHORT QUESTIONS
            //
            // Examples:
            // "Why?"
            // "How?"
            // "What about Singleton?"
            // "And middleware?"
            // "Why is that?"
            // =========================================================

            string[] words =
                current.Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= 6)
                return true;

            // =========================================================
            // CONTEXT-DEPENDENT WORDING
            // =========================================================

            string lower =
                current.ToLowerInvariant();

            string[] contextIndicators =
            {
        "why is that",
        "why that",
        "how about",
        "what about",
        "what does that",
        "what is that",
        "how does that",
        "how can that",
        "can you explain that",
        "explain that",
        "and what",
        "and how",
        "and why",
        "then what",
        "what about it",
        "what about this",
        "how about this",
        "what does it",
        "how does it",
        "why does it",
        "why do we",
        "how do we",
        "what if"
    };

            foreach (string indicator in contextIndicators)
            {
                if (lower.Contains(indicator))
                    return true;
            }

            // =========================================================
            // PRONOUN / REFERENCE BASED FOLLOW-UP
            // =========================================================

            string[] referenceWords =
            {
        "this",
        "that",
        "these",
        "those",
        "it",
        "they",
        "them",
        "same",
        "above",
        "previous",
        "earlier"
    };

            foreach (string word in referenceWords)
            {
                if (ContainsWholeWord(lower, word))
                    return true;
            }

            // =========================================================
            // SEMANTIC WORD OVERLAP
            //
            // If the current question shares meaningful words with the
            // previous question/answer, keep the previous turn.
            // =========================================================

            HashSet<string> previousWords =
                GetMeaningfulWords(
                    previousQuestion + " " + previousAnswer);

            HashSet<string> currentWords =
                GetMeaningfulWords(
                    current);

            int overlap =
                0;

            foreach (string word in currentWords)
            {
                if (previousWords.Contains(word))
                {
                    overlap++;
                }
            }

            return overlap >= 2;
        }

        private bool ContainsWholeWord(
    string text,
    string word)
        {
            if (string.IsNullOrWhiteSpace(text) ||
                string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            string[] words =
                text.Split(
                    new[] { ' ', '\t', '\r', '\n', '.', ',', '?', '!', ':', ';', '(', ')' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (string item in words)
            {
                if (string.Equals(
                    item,
                    word,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private HashSet<string> GetMeaningfulWords(
    string text)
        {
            HashSet<string> words =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(text))
                return words;

            string[] stopWords =
            {
        "the",
        "a",
        "an",
        "is",
        "are",
        "was",
        "were",
        "what",
        "why",
        "how",
        "when",
        "where",
        "which",
        "who",
        "can",
        "could",
        "would",
        "should",
        "do",
        "does",
        "did",
        "and",
        "or",
        "to",
        "of",
        "in",
        "on",
        "for",
        "with",
        "about",
        "from",
        "this",
        "that",
        "it",
        "they",
        "them",
        "i",
        "you",
        "we",
        "me",
        "my",
        "your"
    };

            string[] tokens =
                text.ToLowerInvariant()
                    .Split(
                        new[]
                        {
                    ' ',
                    '\t',
                    '\r',
                    '\n',
                    '.',
                    ',',
                    '?',
                    '!',
                    ':',
                    ';',
                    '(',
                    ')',
                    '[',
                    ']',
                    '{',
                    '}',
                    '/',
                    '\\',
                    '-'
                        },
                        StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in tokens)
            {
                string word =
                    token.Trim();

                if (word.Length < 3)
                    continue;

                if (stopWords.Contains(word))
                    continue;

                words.Add(word);
            }

            return words;
        }
    }
}
