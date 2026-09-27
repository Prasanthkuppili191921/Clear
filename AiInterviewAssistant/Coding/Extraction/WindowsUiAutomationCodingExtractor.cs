using AiInterviewAssistant.Coding.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace AiInterviewAssistant.Coding.Extraction
{
    public sealed class WindowsUiAutomationCodingExtractor
        : ICodingPageExtractor
    {
        private const int MaxElements = 20000;

        private const double TopIgnoredPixels = 110;

        public Task<CodingPageExtractionResult> ExtractAsync(
            CancellationToken cancellationToken)
        {
            return Task.Run(
                () => Extract(cancellationToken),
                cancellationToken);
        }

        private CodingPageExtractionResult Extract(
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                AutomationElement window =
                    GetActiveWindow();

                if (window == null)
                {
                    return CreateFailure(
                        "Active browser window not found.");
                }

                string pageText =
                    ReadVisibleQuestionContent(
                        window,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(pageText))
                {
                    return CreateFailure(
                        "No visible coding question content was found.");
                }

                DebugLog(
                    "Visible coding question text length: " +
                    pageText.Length);

                CodingSectionParser parser =
                    new CodingSectionParser();

                CodingSections sections =
                    parser.Parse(pageText);

                StarterCodeExtractor starterExtractor =
                    new StarterCodeExtractor();

                string starterCode =
                    starterExtractor.Extract(
                        window);

                CodingExampleParser exampleParser =
                    new CodingExampleParser();

                List<CodingExample> examples =
                    exampleParser.Parse(
                        sections.Examples);

                CodingProblem problem =
                    new CodingProblem
                    {
                        ProblemStatement =
                            sections.Question,

                        Examples =
                            examples,

                        Constraints =
                            sections.Constraints,

                        StarterCode =
                            starterCode,

                        SourceApplication =
                            GetWindowName(window)
                    };

                bool hasQuestion =
                    !string.IsNullOrWhiteSpace(
                        problem.ProblemStatement);

                bool hasExamples =
                    problem.Examples != null &&
                    problem.Examples.Count > 0;

                bool hasConstraints =
                    !string.IsNullOrWhiteSpace(
                        problem.Constraints);

                bool hasStarterCode =
                    !string.IsNullOrWhiteSpace(
                        problem.StarterCode);

                DebugLog(
                    "Coding extraction result: " +
                    "Question=" + hasQuestion +
                    ", Examples=" + hasExamples +
                    ", Constraints=" + hasConstraints +
                    ", StarterCode=" + hasStarterCode);

                if (!hasQuestion &&
                    !hasStarterCode &&
                    !hasExamples &&
                    !hasConstraints)
                {
                    return CreateFailure(
                        "Coding page content could not be extracted.");
                }

                return new CodingPageExtractionResult
                {
                    Problem = problem,

                    UiAutomationSucceeded = true,

                    RequiresVisionFallback =
                        !hasQuestion ||
                        !hasStarterCode,

                    FailureReason = null
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                DebugLog(
                    "Extract ERROR: " +
                    ex);

                return CreateFailure(
                    ex.Message);
            }
        }

        private string ReadVisibleQuestionContent(
            AutomationElement window,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (window == null)
            {
                return string.Empty;
            }

            Rect windowBounds =
                SafeGetBoundingRectangle(
                    window);

            if (windowBounds.IsEmpty)
            {
                return string.Empty;
            }

            double contentTop =
                windowBounds.Top +
                TopIgnoredPixels;

            Rect contentBounds =
                new Rect(
                    windowBounds.Left,
                    contentTop,
                    windowBounds.Width,
                    Math.Max(
                        0,
                        windowBounds.Bottom -
                        contentTop));

            DebugLog(
                "Window bounds: " +
                windowBounds);

            DebugLog(
                "Visible content bounds: " +
                contentBounds);

            var candidates =
                new List<VisibleTextCandidate>();

            AutomationElementCollection elements;

            try
            {
                elements =
                    window.FindAll(
                        TreeScope.Descendants,
                        System.Windows.Automation.Condition.TrueCondition);
            }
            catch (Exception ex)
            {
                DebugLog(
                    "FindAll ERROR: " +
                    ex.Message);

                return string.Empty;
            }

            int count = 0;

            foreach (
                AutomationElement element
                in elements)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                if (count++ >= MaxElements)
                {
                    break;
                }

                try
                {
                    if (!IsRelevantTextElement(
                            element))
                    {
                        continue;
                    }

                    Rect bounds =
                        SafeGetBoundingRectangle(
                            element);

                    if (bounds.IsEmpty)
                    {
                        continue;
                    }

                    if (!contentBounds.IntersectsWith(
                            bounds))
                    {
                        continue;
                    }

                    if (bounds.Bottom <= contentTop)
                    {
                        continue;
                    }

                    string text =
                        ReadTextPattern(
                            element);

                    if (string.IsNullOrWhiteSpace(
                            text))
                    {
                        continue;
                    }

                    text =
                        NormalizeText(
                            text);

                    if (string.IsNullOrWhiteSpace(
                            text))
                    {
                        continue;
                    }

                    candidates.Add(
                        new VisibleTextCandidate
                        {
                            Text = text,
                            Top = bounds.Top,
                            Left = bounds.Left,
                            Width = bounds.Width,
                            Height = bounds.Height,
                            ControlType =
                                SafeGetControlType(
                                    element)
                        });
                }
                catch
                {
                }
            }

            candidates.Sort(
                CompareCandidates);

            var blocks =
                new List<string>();

            foreach (
                VisibleTextCandidate candidate
                in candidates)
            {
                if (candidate == null ||
                    string.IsNullOrWhiteSpace(
                        candidate.Text))
                {
                    continue;
                }

                AddUniqueVisibleBlock(
                    blocks,
                    candidate.Text);
            }

            StringBuilder result =
                new StringBuilder();

            foreach (string block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block))
                {
                    continue;
                }

                if (result.Length > 0)
                {
                    result.AppendLine();
                    result.AppendLine();
                }

                result.Append(
                    block.Trim());
            }

            string finalText =
                result
                    .ToString()
                    .Trim();

            DebugLog(
                "Visible question blocks: " +
                blocks.Count);

            DebugLog(
                "Visible question final length: " +
                finalText.Length);

            return finalText;
        }

        private bool IsRelevantTextElement(
            AutomationElement element)
        {
            if (element == null)
            {
                return false;
            }

            try
            {
                ControlType type =
                    element.Current.ControlType;

                if (type == ControlType.Text ||
                    type == ControlType.Document ||
                    type == ControlType.Edit)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string ReadTextPattern(
            AutomationElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

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

                string text =
                    range.GetText(-1);

                return NormalizeText(
                    text);
            }
            catch
            {
                return string.Empty;
            }
        }

        private Rect SafeGetBoundingRectangle(
            AutomationElement element)
        {
            try
            {
                return element
                    .Current
                    .BoundingRectangle;
            }
            catch
            {
                return Rect.Empty;
            }
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
                return ControlType.Custom;
            }
        }

        private void AddUniqueVisibleBlock(
            List<string> blocks,
            string text)
        {
            if (blocks == null ||
                string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string cleaned =
                NormalizeText(text);

            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return;
            }

            foreach (string existing in blocks)
            {
                if (string.Equals(
                    existing,
                    cleaned,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (existing.Length > 100 &&
                    cleaned.Length > 100)
                {
                    if (existing.IndexOf(
                            cleaned,
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cleaned.IndexOf(
                            existing,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return;
                    }
                }
            }

            blocks.Add(
                cleaned);
        }

        private int CompareCandidates(
            VisibleTextCandidate x,
            VisibleTextCandidate y)
        {
            int topComparison =
                x.Top.CompareTo(
                    y.Top);

            if (topComparison != 0)
            {
                return topComparison;
            }

            return x.Left.CompareTo(
                y.Left);
        }

        private string NormalizeText(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string value =
                text.Replace(
                    "\r\n",
                    "\n");

            value =
                value.Replace(
                    "\r",
                    "\n");

            string[] lines =
                value.Split(
                    new[]
                    {
                        '\n'
                    },
                    StringSplitOptions.None);

            StringBuilder builder =
                new StringBuilder();

            foreach (string rawLine in lines)
            {
                string line =
                    rawLine.TrimEnd();

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }

                    continue;
                }

                builder.AppendLine(
                    line);
            }

            return builder
                .ToString()
                .Trim();
        }

        private AutomationElement GetActiveWindow()
        {
            try
            {
                AutomationElement current =
                    AutomationElement.FocusedElement;

                if (current == null)
                {
                    return null;
                }

                for (
                    int i = 0;
                    i < 50 &&
                    current != null;
                    i++)
                {
                    if (current.Current.ControlType ==
                        ControlType.Window)
                    {
                        return current;
                    }

                    current =
                        TreeWalker.RawViewWalker
                            .GetParent(
                                current);
                }
            }
            catch
            {
            }

            return null;
        }

        private string GetWindowName(
            AutomationElement window)
        {
            try
            {
                return window.Current.Name ??
                       string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private CodingPageExtractionResult CreateFailure(
            string message)
        {
            return new CodingPageExtractionResult
            {
                Problem = null,

                UiAutomationSucceeded = false,

                RequiresVisionFallback = true,

                FailureReason =
                    string.IsNullOrWhiteSpace(message)
                        ? "Coding page extraction failed."
                        : message
            };
        }

        private void DebugLog(
            string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "[CodingExtractor] " +
                    message);
            }
            catch
            {
            }
        }

        private sealed class VisibleTextCandidate
        {
            public string Text { get; set; }

            public double Top { get; set; }

            public double Left { get; set; }

            public double Width { get; set; }

            public double Height { get; set; }

            public ControlType ControlType { get; set; }
        }
    }
}