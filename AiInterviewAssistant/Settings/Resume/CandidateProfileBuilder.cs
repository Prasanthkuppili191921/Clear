using System;
using System.Collections.Generic;
using System.Text;

namespace AiInterviewAssistant.Settings.Resume
{
    public static class CandidateProfileBuilder
    {
        public static CandidateProfile Build(
            string resumeText)
        {
            CandidateProfile profile =
                new CandidateProfile();

            if (string.IsNullOrWhiteSpace(resumeText))
                return profile;

            string[] lines =
                resumeText
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Split(
                        new[] { '\n' },
                        StringSplitOptions.None);

            StringBuilder current =
                new StringBuilder();

            string section = "SUMMARY";

            foreach (string rawLine in lines)
            {
                string line =
                    rawLine == null
                        ? ""
                        : rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string detected =
                    DetectSection(line);

                if (!string.IsNullOrWhiteSpace(detected))
                {
                    SaveSection(
                        profile,
                        section,
                        current.ToString());

                    section = detected;

                    current.Clear();

                    continue;
                }

                if (current.Length > 0)
                    current.AppendLine();

                current.Append(line);
            }

            SaveSection(
                profile,
                section,
                current.ToString());

            return profile;
        }


        private static string DetectSection(
            string line)
        {
            string value =
                line.Trim()
                    .Trim(':')
                    .ToLowerInvariant();

            if (value == "summary" ||
                value == "professional summary" ||
                value == "profile" ||
                value == "career summary")
            {
                return "SUMMARY";
            }

            if (value == "experience" ||
                value == "work experience" ||
                value == "professional experience" ||
                value == "employment history")
            {
                return "EXPERIENCE";
            }

            if (value == "skills" ||
                value == "technical skills" ||
                value == "core skills" ||
                value == "technical expertise")
            {
                return "SKILLS";
            }

            if (value == "projects" ||
                value == "key projects" ||
                value == "project experience")
            {
                return "PROJECTS";
            }

            if (value == "education" ||
                value == "academic background")
            {
                return "EDUCATION";
            }

            if (value == "certifications" ||
                value == "certificates")
            {
                return "CERTIFICATIONS";
            }

            return "";
        }


        private static void SaveSection(
            CandidateProfile profile,
            string section,
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            string value =
                text.Trim();

            switch (section)
            {
                case "SUMMARY":
                    profile.Summary +=
                        Append(profile.Summary, value);
                    break;

                case "EXPERIENCE":
                    profile.Experience +=
                        Append(profile.Experience, value);
                    break;

                case "SKILLS":
                    profile.Skills +=
                        Append(profile.Skills, value);
                    break;

                case "PROJECTS":
                    profile.Projects +=
                        Append(profile.Projects, value);
                    break;

                case "EDUCATION":
                    profile.Education +=
                        Append(profile.Education, value);
                    break;

                case "CERTIFICATIONS":
                    profile.Certifications +=
                        Append(
                            profile.Certifications,
                            value);
                    break;
            }
        }


        private static string Append(
            string existing,
            string value)
        {
            if (string.IsNullOrWhiteSpace(existing))
                return value;

            return "\n" + value;
        }
    }
}