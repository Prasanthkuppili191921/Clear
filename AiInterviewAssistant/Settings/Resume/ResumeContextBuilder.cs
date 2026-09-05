using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AiInterviewAssistant.Settings.Resume
{
    public static class ResumeContextBuilder
    {
        // =========================================================
        // BUILD COMPACT RESUME CONTEXT
        // =========================================================

        public static string Build(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
                return string.Empty;

            string text = resumeText;

            // -----------------------------------------------------
            // Normalize line endings
            // -----------------------------------------------------

            text = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            // -----------------------------------------------------
            // Remove trailing spaces from every line
            // -----------------------------------------------------

            string[] lines =
                text.Split(
                    new[] { '\n' },
                    StringSplitOptions.None);

            StringBuilder builder =
                new StringBuilder();

            bool previousLineWasEmpty = false;

            foreach (string rawLine in lines)
            {
                if (rawLine == null)
                    continue;

                string line =
                    rawLine.Trim();

                // -------------------------------------------------
                // Remove excessive whitespace inside a line
                // -------------------------------------------------

                line =
                    Regex.Replace(
                        line,
                        @"[ \t]+",
                        " ");

                // -------------------------------------------------
                // Keep only one blank line between sections
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (previousLineWasEmpty)
                        continue;

                    previousLineWasEmpty = true;

                    builder.AppendLine();

                    continue;
                }

                previousLineWasEmpty = false;

                builder.AppendLine(line);
            }

            // -----------------------------------------------------
            // Final cleanup
            // -----------------------------------------------------

            string result =
                builder.ToString().Trim();

            // -----------------------------------------------------
            // Prevent accidental excessive blank lines
            // -----------------------------------------------------

            result =
                Regex.Replace(
                    result,
                    @"\n{3,}",
                    "\n\n");

            return result.Trim();
        }
    }
}