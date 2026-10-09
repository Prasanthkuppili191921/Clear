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
            // INCLUDE THE IMMEDIATELY PREVIOUS COMPLETE TURN
            // =========================================================
            // The model receives one previous turn so it can resolve natural
            // follow-ups that do not match a hardcoded phrase list. The system
            // prompt instructs it not to reuse that context for independent questions.

            bool usePreviousTurn =
                previousUserIndex >= 0 &&
                previousAssistantIndex >= 0;

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

            string current = currentQuestion
                .Trim()
                .ToLowerInvariant()
                .TrimEnd('.', '?', '!', ' ');

            // No previous turn means this cannot be a follow-up.
            if (string.IsNullOrWhiteSpace(previousQuestion) ||
                string.IsNullOrWhiteSpace(previousAnswer))
            {
                return false;
            }

            // Only clearly context-dependent questions use previous context.
            string[] explicitFollowUps =
            {
                "why",
                "how",
                "why is that",
                "how is that",
                "why so",
                "how so",
                "explain that",
                "explain this",
                "can you explain that",
                "can you explain this",
                "elaborate",
                "tell me more",
                "give an example",
                "show an example",
                "what do you mean",
                "what about that",
                "what about this",
                "and why",
                "and how",
                "then what",
                "what happens next"
            };

            foreach (string phrase in explicitFollowUps)
            {
                if (current == phrase)
                    return true;
            }

            // Require an explicit reference AND a context-dependent phrase.
            // Words like "this", "that", or "it" alone are insufficient.
            string[] dependentPhrases =
            {
                "why does that",
                "why is that",
                "how does that",
                "how is that",
                "what does that mean",
                "what happens to it",
                "how does it work",
                "why does it happen",
                "can you explain it",
                "can you explain that",
                "how about that",
                "what about that",
                "what about this",
                "in that case",
                "based on that"
            };

            foreach (string phrase in dependentPhrases)
            {
                if (current.Contains(phrase))
                    return true;
            }

            // All other questions are independent by default.
            // Do not use keyword overlap as proof of a follow-up.
            return false;
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
