using AiInterviewAssistant.Coding.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using Condition = System.Windows.Automation.Condition;

namespace AiInterviewAssistant.Coding.Extraction
{
    public sealed class WindowsUiAutomationCodingExtractor
        : ICodingPageExtractor
    {
        private const int MaxElements = 20000;

        private const double TopIgnoredPixels = 110;

        private const int MaxExtractionAttempts = 3;

        private const int RetryDelayMilliseconds = 250;

        public Task<CodingPageExtractionResult> ExtractAsync(
            CancellationToken cancellationToken)
        {
            return Task.Run(
                () => ExtractWithRetry(
                    cancellationToken),
                cancellationToken);
        }

        private CodingPageExtractionResult ExtractWithRetry(
            CancellationToken cancellationToken)
        {
            CodingPageExtractionResult lastResult = null;

            for (
                int attempt = 1;
                attempt <= MaxExtractionAttempts;
                attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DebugLog(
                    "Extraction attempt " +
                    attempt +
                    "/" +
                    MaxExtractionAttempts);

                lastResult =
                    Extract(
                        cancellationToken);

                if (lastResult != null &&
                    lastResult.Problem != null &&
                    !string.IsNullOrWhiteSpace(
                        lastResult.Problem.ProblemStatement))
                {
                    DebugLog(
                        "Extraction succeeded on attempt " +
                        attempt);

                    return lastResult;
                }

                DebugLog(
                    "Extraction attempt " +
                    attempt +
                    " returned no usable question.");

                if (attempt <
                    MaxExtractionAttempts)
                {
                    Thread.Sleep(
                        RetryDelayMilliseconds);
                }
            }

            DebugLog(
                "All extraction attempts failed.");

            return lastResult ??
                   CreateFailure(
                       "Coding page extraction failed.");
        }

        private CodingPageExtractionResult Extract(
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                AutomationElement window =
                    FindCodingBrowserWindow(
                        cancellationToken);

                if (window == null)
                {
                    DebugLog(
                        "Coding browser window not found.");

                    return CreateFailure(
                        "Coding browser window not found.");
                }

                string windowName =
                    GetWindowName(window);

                DebugLog(
                    "Target coding window: " +
                    windowName);

                string pageText =
                    ReadVisibleQuestionContent(
                        window,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(pageText))
                {
                    DebugLog(
                        "No visible coding question content found.");

                    return CreateFailure(
                        "No visible coding question content was found.");
                }

                DebugLog(
                    "Visible coding question text length: " +
                    pageText.Length);

                CodingSectionParser parser =
                    new CodingSectionParser();

                CodingSections sections =
                    parser.Parse(
                        pageText);

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
                    "Question=" +
                    hasQuestion +
                    ", Examples=" +
                    hasExamples +
                    ", Constraints=" +
                    hasConstraints +
                    ", StarterCode=" +
                    hasStarterCode);

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

        private AutomationElement FindCodingBrowserWindow(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            /*
             * First try the currently focused window.
             * This keeps the normal/non-Record-Interview flow fast.
             */
            AutomationElement focusedWindow =
                GetFocusedWindow();

            if (focusedWindow != null)
            {
                string focusedText =
                    TryReadCompleteDocumentText(
                        focusedWindow,
                        cancellationToken);

                if (LooksLikeCodingPage(
                        focusedWindow,
                        focusedText))
                {
                    DebugLog(
                        "Focused window is the coding window.");

                    return focusedWindow;
                }
            }

            /*
             * Important:
             *
             * Record Interview can keep focus on our application.
             * Therefore we enumerate top-level windows and look for
             * the actual browser/coding window.
             */
            List<AutomationElement> windows =
                GetTopLevelWindows();

            AutomationElement bestWindow = null;

            int bestScore = 0;

            foreach (
                AutomationElement window
                in windows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (window == null)
                {
                    continue;
                }

                try
                {
                    if (!IsVisibleWindow(window))
                    {
                        continue;
                    }

                    string name =
                        GetWindowName(window);

                    if (IsOurApplicationWindow(
                        window))
                    {
                        continue;
                    }

                    string documentText =
                        TryReadCompleteDocumentText(
                            window,
                            cancellationToken);

                    int score =
                        GetCodingWindowScore(
                            window,
                            name,
                            documentText);

                    DebugLog(
                        "Window candidate: [" +
                        name +
                        "] score=" +
                        score);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestWindow = window;
                    }
                }
                catch
                {
                }
            }

            if (bestWindow != null)
            {
                DebugLog(
                    "Selected coding window with score=" +
                    bestScore);

                return bestWindow;
            }

            /*
             * Last fallback:
             * return focused window if we have one.
             *
             * This preserves the original behaviour for cases where
             * the browser does not expose enough top-level information.
             */
            return focusedWindow;
        }

        private int GetCodingWindowScore(
            AutomationElement window,
            string windowName,
            string documentText)
        {
            int score = 0;

            if (string.IsNullOrWhiteSpace(
                windowName) &&
                string.IsNullOrWhiteSpace(
                    documentText))
            {
                return 0;
            }

            if (ContainsCodingKeyword(
                windowName))
            {
                score += 5;
            }

            if (!string.IsNullOrWhiteSpace(
                documentText))
            {
                string value =
                    documentText.ToLowerInvariant();

                if (value.Contains("constraints"))
                {
                    score += 2;
                }

                if (value.Contains("examples"))
                {
                    score += 2;
                }

                if (value.Contains("problem"))
                {
                    score += 2;
                }

                if (value.Contains("function"))
                {
                    score += 2;
                }

                if (value.Contains("input"))
                {
                    score += 1;
                }

                if (value.Contains("output"))
                {
                    score += 1;
                }

                if (value.Contains("starter code"))
                {
                    score += 3;
                }

                if (value.Contains("run code"))
                {
                    score += 3;
                }

                if (value.Contains("submit code"))
                {
                    score += 3;
                }

                if (value.Contains("leetcode"))
                {
                    score += 5;
                }

                if (value.Contains("hackerrank"))
                {
                    score += 5;
                }

                if (value.Contains("codesignal"))
                {
                    score += 5;
                }

                if (value.Contains("codility"))
                {
                    score += 5;
                }
            }

            return score;
        }

        private bool LooksLikeCodingPage(
            AutomationElement window,
            string documentText)
        {
            if (window == null)
            {
                return false;
            }

            if (ContainsCodingKeyword(
                GetWindowName(window)))
            {
                return true;
            }

            return HasStrongCodingSignals(
                documentText);
        }

        private AutomationElement GetFocusedWindow()
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
                    i < 50 &&
                    current != null;
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

        private List<AutomationElement> GetTopLevelWindows()
        {
            var result =
                new List<AutomationElement>();

            try
            {
                NativeMethods.EnumWindows(
                    delegate (
                        IntPtr hWnd,
                        IntPtr lParam)
                    {
                        try
                        {
                            if (hWnd == IntPtr.Zero)
                            {
                                return true;
                            }

                            if (!NativeMethods.IsWindowVisible(
                                hWnd))
                            {
                                return true;
                            }

                            AutomationElement element =
                                AutomationElement.FromHandle(
                                    hWnd);

                            if (element != null)
                            {
                                result.Add(
                                    element);
                            }
                        }
                        catch
                        {
                        }

                        return true;
                    },
                    IntPtr.Zero);
            }
            catch (Exception ex)
            {
                DebugLog(
                    "EnumWindows ERROR: " +
                    ex.Message);
            }

            return result;
        }

        private bool IsVisibleWindow(
            AutomationElement window)
        {
            if (window == null)
            {
                return false;
            }

            try
            {
                return window.Current.IsEnabled &&
                       !SafeGetBoundingRectangle(
                           window).IsEmpty;
            }
            catch
            {
                return false;
            }
        }

        private bool IsOurApplicationWindow(
            AutomationElement window)
        {
            try
            {
                int processId =
                    window.Current.ProcessId;

                using (
                    Process process =
                        Process.GetProcessById(
                            processId))
                {
                    string currentProcessName =
                        Process.GetCurrentProcess()
                            .ProcessName;

                    return string.Equals(
                        process.ProcessName,
                        currentProcessName,
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
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
                        Condition.TrueCondition);
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
                if (string.IsNullOrWhiteSpace(
                    block))
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

        private string TryReadCompleteDocumentText(
            AutomationElement window,
            CancellationToken cancellationToken)
        {
            try
            {
                if (window == null)
                {
                    return string.Empty;
                }

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

                    if (!string.IsNullOrWhiteSpace(
                        text))
                    {
                        return text;
                    }
                }

                string windowText =
                    ReadTextPattern(
                        window);

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

                    if (!string.IsNullOrWhiteSpace(
                        text) &&
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

                return type == ControlType.Text ||
                       type == ControlType.Document ||
                       type == ControlType.Edit;
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

        private AutomationElement SafeGetParent(
            AutomationElement element)
        {
            try
            {
                return TreeWalker.RawViewWalker
                    .GetParent(
                        element);
            }
            catch
            {
                return null;
            }
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

        private void AddUniqueVisibleBlock(
            List<string> blocks,
            string text)
        {
            if (blocks == null ||
                string.IsNullOrWhiteSpace(
                    text))
            {
                return;
            }

            string cleaned =
                NormalizeText(
                    text);

            if (string.IsNullOrWhiteSpace(
                cleaned))
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
            if (string.IsNullOrWhiteSpace(
                text))
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

                if (string.IsNullOrWhiteSpace(
                    line))
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

        private bool HasStrongCodingSignals(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return false;
            }

            string value =
                text.ToLowerInvariant();

            int score = 0;

            string[] keywords =
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

            foreach (string keyword in keywords)
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

            return value.Contains("constraints") &&
                   value.Contains("examples") &&
                   (value.Contains("function") ||
                    value.Contains("code") ||
                    value.Contains("input"));
        }

        private bool ContainsCodingKeyword(
            string text)
        {
            if (string.IsNullOrWhiteSpace(
                text))
            {
                return false;
            }

            string value =
                text.ToLowerInvariant();

            string[] keywords =
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

            foreach (string keyword in keywords)
            {
                if (value.Contains(
                    keyword))
                {
                    return true;
                }
            }

            return false;
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
                    string.IsNullOrWhiteSpace(
                        message)
                        ? "Coding page extraction failed."
                        : message
            };
        }

        private void DebugLog(
            string message)
        {
            try
            {
                Debug.WriteLine(
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

        private static class NativeMethods
        {
            public delegate bool EnumWindowsProc(
                IntPtr hWnd,
                IntPtr lParam);

            [DllImport(
                "user32.dll",
                SetLastError = true)]
            [return: MarshalAs(
                UnmanagedType.Bool)]
            public static extern bool EnumWindows(
                EnumWindowsProc lpEnumFunc,
                IntPtr lParam);

            [DllImport(
                "user32.dll")]
            [return: MarshalAs(
                UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(
                IntPtr hWnd);
        }
    }
}