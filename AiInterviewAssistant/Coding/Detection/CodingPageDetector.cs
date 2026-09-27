using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace AiInterviewAssistant.Coding.Detection
{
    public sealed class CodingPageDetector
        : ICodingPageDetector
    {
        private static readonly string[] CodingKeywords =
        {
            "hackerrank",
            "leetcode",
            "codesignal",
            "codility",
            "coding challenge",
            "code challenge",
            "programming problem",
            "algorithm",
            "starter code",
            "run code",
            "submit code",
            "test cases",
            "testcase",
            "constraints",
            "examples",
            "function signature",
            "input format",
            "output format",
            "problem statement"
        };

        public Task<bool> IsCodingPageAsync(
            CancellationToken cancellationToken)
        {
            return Task.Run(
                () =>
                    DetectCodingPage(
                        cancellationToken),
                cancellationToken);
        }

        private bool DetectCodingPage(
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                AutomationElement window =
                    GetActiveWindow();

                if (window == null)
                {
                    return false;
                }

                string windowName =
                    SafeGetName(window);

                if (ContainsCodingKeyword(
                    windowName))
                {
                    return true;
                }

                string documentText =
                    TryReadCompleteDocumentText(
                        window,
                        cancellationToken);

                if (!string.IsNullOrWhiteSpace(
                    documentText))
                {
                    if (HasStrongCodingSignals(
                        documentText))
                    {
                        return true;
                    }
                }

                var texts =
                    new List<string>();

                AutomationElementCollection descendants =
                    window.FindAll(
                        TreeScope.Descendants,
                        Condition.TrueCondition);

                foreach (
                    AutomationElement element
                    in descendants)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    AddElementText(
                        element,
                        texts);
                }

                return HasCodingSignals(
                    texts);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        private string TryReadCompleteDocumentText(
            AutomationElement window,
            CancellationToken cancellationToken)
        {
            try
            {
                AutomationElement document =
                    window.FindFirst(
                        TreeScope.Descendants,
                        new PropertyCondition(
                            AutomationElement.ControlTypeProperty,
                            ControlType.Document));

                if (document != null)
                {
                    string text =
                        ReadTextPattern(
                            document);

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }

                string windowText =
                    ReadTextPattern(window);

                if (!string.IsNullOrWhiteSpace(
                    windowText))
                {
                    return windowText;
                }

                AutomationElementCollection descendants =
                    window.FindAll(
                        TreeScope.Descendants,
                        Condition.TrueCondition);

                string longest =
                    string.Empty;

                foreach (
                    AutomationElement element
                    in descendants)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    string text =
                        ReadTextPattern(
                            element);

                    if (!string.IsNullOrWhiteSpace(text) &&
                        text.Length > longest.Length)
                    {
                        longest = text;
                    }
                }

                return longest;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string ReadTextPattern(
            AutomationElement element)
        {
            try
            {
                object patternObject;

                if (!element.TryGetCurrentPattern(
                    TextPattern.Pattern,
                    out patternObject))
                {
                    return string.Empty;
                }

                TextPattern pattern =
                    patternObject as TextPattern;

                if (pattern == null)
                {
                    return string.Empty;
                }

                TextPatternRange range =
                    pattern.DocumentRange;

                if (range == null)
                {
                    return string.Empty;
                }

                return range
                    .GetText(-1)
                    ?.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private AutomationElement GetActiveWindow()
        {
            try
            {
                AutomationElement focused =
                    AutomationElement.FocusedElement;

                if (focused == null)
                {
                    return null;
                }

                AutomationElement current =
                    focused;

                for (
                    int i = 0;
                    i < 40 && current != null;
                    i++)
                {
                    if (SafeGetControlType(current) ==
                        ControlType.Window)
                    {
                        return current;
                    }

                    current =
                        SafeGetParent(current);
                }
            }
            catch
            {
            }

            return null;
        }

        private bool HasStrongCodingSignals(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string value =
                text.ToLowerInvariant();

            int score = 0;

            foreach (
                string keyword
                in CodingKeywords)
            {
                if (value.Contains(
                    keyword))
                {
                    score++;
                }
            }

            if (score >= 2)
            {
                return true;
            }

            if (value.Contains("constraints") &&
                value.Contains("examples") &&
                (value.Contains("function") ||
                 value.Contains("code") ||
                 value.Contains("input")))
            {
                return true;
            }

            return false;
        }

        private bool HasCodingSignals(
            List<string> texts)
        {
            if (texts == null ||
                texts.Count == 0)
            {
                return false;
            }

            int score = 0;

            bool hasCode =
                false;

            bool hasProblem =
                false;

            bool hasConstraints =
                false;

            bool hasExamples =
                false;

            foreach (
                string rawText
                in texts)
            {
                if (string.IsNullOrWhiteSpace(
                    rawText))
                {
                    continue;
                }

                string text =
                    rawText
                        .Trim()
                        .ToLowerInvariant();

                foreach (
                    string keyword
                    in CodingKeywords)
                {
                    if (text.Contains(keyword))
                    {
                        score++;
                        break;
                    }
                }

                if (text.Contains("starter code") ||
                    text.Contains("run code") ||
                    text.Contains("submit code") ||
                    text.Contains("function"))
                {
                    hasCode = true;
                }

                if (text.Contains("problem") ||
                    text.Contains("description"))
                {
                    hasProblem = true;
                }

                if (text.Contains("constraints"))
                {
                    hasConstraints = true;
                }

                if (text.Contains("example"))
                {
                    hasExamples = true;
                }
            }

            if (score >= 2 &&
                (hasCode || hasProblem))
            {
                return true;
            }

            int structuralSignals = 0;

            if (hasCode)
                structuralSignals++;

            if (hasProblem)
                structuralSignals++;

            if (hasConstraints)
                structuralSignals++;

            if (hasExamples)
                structuralSignals++;

            return structuralSignals >= 3;
        }

        private void AddElementText(
            AutomationElement element,
            List<string> texts)
        {
            try
            {
                string name =
                    SafeGetName(element);

                if (!string.IsNullOrWhiteSpace(name))
                {
                    texts.Add(name);
                }

                string value =
                    SafeGetValue(element);

                if (!string.IsNullOrWhiteSpace(value) &&
                    !string.Equals(
                        value,
                        name,
                        StringComparison.Ordinal))
                {
                    texts.Add(value);
                }
            }
            catch
            {
            }
        }

        private bool ContainsCodingKeyword(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string value =
                text.ToLowerInvariant();

            foreach (
                string keyword
                in CodingKeywords)
            {
                if (value.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        private string SafeGetName(
            AutomationElement element)
        {
            try
            {
                return element.Current.Name ??
                       string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string SafeGetValue(
            AutomationElement element)
        {
            try
            {
                object patternObject;

                if (element.TryGetCurrentPattern(
                    ValuePattern.Pattern,
                    out patternObject))
                {
                    ValuePattern pattern =
                        patternObject as ValuePattern;

                    if (pattern != null)
                    {
                        return pattern.Current.Value ??
                               string.Empty;
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private ControlType SafeGetControlType(
            AutomationElement element)
        {
            try
            {
                return element.Current.ControlType;
            }
            catch
            {
                return null;
            }
        }

        private AutomationElement SafeGetParent(
            AutomationElement element)
        {
            try
            {
                return TreeWalker.RawViewWalker
                    .GetParent(element);
            }
            catch
            {
                return null;
            }
        }
    }
}