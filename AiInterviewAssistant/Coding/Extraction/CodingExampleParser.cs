using AiInterviewAssistant.Coding.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AiInterviewAssistant.Coding.Extraction
{
    public sealed class CodingExampleParser
    {
        public List<CodingExample> Parse(string text)
        {
            var examples = new List<CodingExample>();

            if (string.IsNullOrWhiteSpace(text))
                return examples;

            string normalized =
                text.Replace("\r\n", "\n")
                    .Replace("\r", "\n");

            MatchCollection matches =
                Regex.Matches(
                    normalized,
                    @"(?is)(?:Example|Sample)\s*\d*\s*:?\s*(.*?)(?=(?:Example|Sample)\s*\d*\s*:|$)");

            foreach (Match match in matches)
            {
                string block = match.Groups[1].Value.Trim();

                if (string.IsNullOrWhiteSpace(block))
                    continue;

                var example = ParseExampleBlock(block);

                if (example != null)
                    examples.Add(example);
            }

            return examples;
        }

        private CodingExample ParseExampleBlock(string block)
        {
            if (string.IsNullOrWhiteSpace(block))
                return null;

            string input = string.Empty;
            string output = string.Empty;
            string explanation = string.Empty;

            Match inputMatch =
                Regex.Match(
                    block,
                    @"(?is)\bInput\s*:\s*(.*?)(?=\bOutput\s*:|\bExplanation\s*:|$)");

            if (inputMatch.Success)
            {
                input = inputMatch.Groups[1].Value.Trim();
            }

            Match outputMatch =
                Regex.Match(
                    block,
                    @"(?is)\bOutput\s*:\s*(.*?)(?=\bExplanation\s*:|$)");

            if (outputMatch.Success)
            {
                output = outputMatch.Groups[1].Value.Trim();
            }

            Match explanationMatch =
                Regex.Match(
                    block,
                    @"(?is)\bExplanation\s*:\s*(.*)$");

            if (explanationMatch.Success)
            {
                explanation =
                    explanationMatch.Groups[1].Value.Trim();
            }

            if (string.IsNullOrWhiteSpace(input) &&
                string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            return new CodingExample
            {
                Input = input,
                Output = output,
                Explanation = explanation
            };
        }
    }
}