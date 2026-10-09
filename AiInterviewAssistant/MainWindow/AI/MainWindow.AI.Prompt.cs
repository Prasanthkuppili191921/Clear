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
    string languageInstruction,
    string currentQuestion)
        {
            string answerModeInstruction =
                BuildAnswerModeInstruction(
                    settings.AnswerMode,
                    settings.ResponseLength);

            return InterviewAnswerContext.BuildSystemPrompt(
                settings.ResumeText,
                languageInstruction,
                answerModeInstruction,
                currentQuestion);
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
                    languageInstruction,
                    currentQuestion);

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
            // ADD PREVIOUS TURN WHEN IT IS A FOLLOW-UP
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

            string normalized =
               current
                   .ToLowerInvariant()
                   .Trim();

            if (normalized == "why" ||
                normalized == "how" ||
                normalized == "what" ||
                normalized == "why?" ||
                normalized == "how?" ||
                normalized == "what?" ||
                normalized == "then what?" ||
                normalized == "and then?" ||
                normalized == "how so?" ||
                normalized == "why is that?" ||
                normalized == "how is that?")
            {
                return true;
            }

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
