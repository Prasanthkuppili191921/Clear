using AiInterviewAssistant.Coding.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AiInterviewAssistant.Coding.Extraction
{
    public sealed class CodingSectionParser
    {
        public CodingSections Parse(string text)
        {
            var result = new CodingSections();

            if (string.IsNullOrWhiteSpace(text))
                return result;

            string normalized =
                text.Replace("\r\n", "\n")
                    .Replace("\r", "\n");

            result.Question =
                ExtractSection(
                    normalized,
                    new[]
                    {
                        "Problem",
                        "Problem Statement",
                        "Description"
                    },
                    new[]
                    {
                        "Input",
                        "Input Format",
                        "Output",
                        "Output Format",
                        "Constraints",
                        "Examples",
                        "Example",
                        "Sample",
                        "Starter Code",
                        "Function"
                    });

            result.Input =
                ExtractSection(
                    normalized,
                    new[]
                    {
                        "Input",
                        "Input Format"
                    },
                    new[]
                    {
                        "Output",
                        "Output Format",
                        "Constraints",
                        "Examples",
                        "Example",
                        "Sample",
                        "Starter Code"
                    });

            result.Output =
                ExtractSection(
                    normalized,
                    new[]
                    {
                        "Output",
                        "Output Format"
                    },
                    new[]
                    {
                        "Constraints",
                        "Examples",
                        "Example",
                        "Sample",
                        "Starter Code"
                    });

            result.Constraints =
                ExtractSection(
                    normalized,
                    new[]
                    {
                        "Constraints",
                        "Constraint"
                    },
                    new[]
                    {
                        "Examples",
                        "Example",
                        "Sample",
                        "Starter Code",
                        "Function",
                        "Solution"
                    });

            result.Examples =
                ExtractSection(
                    normalized,
                    new[]
                    {
                        "Examples",
                        "Example",
                        "Samples",
                        "Sample"
                    },
                    new[]
                    {
                        "Explanation",
                        "Constraints",
                        "Starter Code",
                        "Solution"
                    });

            return result;
        }

        private string ExtractSection(
            string text,
            IEnumerable<string> startMarkers,
            IEnumerable<string> endMarkers)
        {
            foreach (string startMarker in startMarkers)
            {
                foreach (string endMarker in endMarkers)
                {
                    Match match =
                        Regex.Match(
                            text,
                            @"(?is)\b" +
                            Regex.Escape(startMarker) +
                            @"\s*:?\s*(.*?)(?=\n\s*" +
                            Regex.Escape(endMarker) +
                            @"\s*:?\s*|\z)");

                    if (match.Success)
                    {
                        string value =
                            Clean(match.Groups[1].Value);

                        if (!string.IsNullOrWhiteSpace(value))
                            return value;
                    }
                }

                Match untilEnd =
                    Regex.Match(
                        text,
                        @"(?is)\b" +
                        Regex.Escape(startMarker) +
                        @"\s*:?\s*(.*)$");

                if (untilEnd.Success)
                {
                    string value =
                        Clean(untilEnd.Groups[1].Value);

                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return string.Empty;
        }

        private string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim();
        }
    }

    public sealed class CodingSections
    {
        public string Question { get; set; }

        public string Input { get; set; }

        public string Output { get; set; }

        public string Constraints { get; set; }

        public string Examples { get; set; }
    }
}