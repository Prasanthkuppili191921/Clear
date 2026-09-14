using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace AiInterviewAssistant
{
    public sealed class InterviewSessionLogger
    {
        // =========================================================
        // FIELDS
        // =========================================================

        private readonly object _sync = new object();

        private readonly string _sessionFilePath;
        private readonly string _questionTemplate;

        // Complete HTML is maintained in memory during the session.
        private string _sessionHtml;

        private bool _sessionEnded;
        private int _questionNumber;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public InterviewSessionLogger()
        {
            try
            {
                // -------------------------------------------------
                // Find AiInterviewAssistant folder
                // -------------------------------------------------

                string aiInterviewAssistantFolder =
                    FindAiInterviewAssistantFolder();


                // -------------------------------------------------
                // Template folder
                // -------------------------------------------------

                string templateFolder =
                    Path.Combine(
                        aiInterviewAssistantFolder,
                        "InterviewTemplates");


                string sessionTemplatePath =
                    Path.Combine(
                        templateFolder,
                        "InterviewSessionTemplate.html");


                string questionTemplatePath =
                    Path.Combine(
                        templateFolder,
                        "InterviewQuestionTemplate.html");


                string cssPath =
                    Path.Combine(
                        templateFolder,
                        "InterviewSession.css");


                // -------------------------------------------------
                // Validate templates
                // -------------------------------------------------

                if (!File.Exists(sessionTemplatePath))
                {
                    throw new FileNotFoundException(
                        "InterviewSessionTemplate.html was not found.",
                        sessionTemplatePath);
                }


                if (!File.Exists(questionTemplatePath))
                {
                    throw new FileNotFoundException(
                        "InterviewQuestionTemplate.html was not found.",
                        questionTemplatePath);
                }


                if (!File.Exists(cssPath))
                {
                    throw new FileNotFoundException(
                        "InterviewSession.css was not found.",
                        cssPath);
                }


                // -------------------------------------------------
                // Read templates
                // -------------------------------------------------

                string sessionTemplate =
                    File.ReadAllText(
                        sessionTemplatePath,
                        Encoding.UTF8);


                _questionTemplate =
                    File.ReadAllText(
                        questionTemplatePath,
                        Encoding.UTF8);


                string css =
                    File.ReadAllText(
                        cssPath,
                        Encoding.UTF8);


                // -------------------------------------------------
                // Find parent/root folder
                //
                // Expected:
                //
                // C:\Invisible app\
                //     AiInterviewAssistant\
                //         AiInterviewAssistant\
                //
                // InterviewSessions should be:
                //
                // C:\Invisible app\
                //     AiInterviewAssistant\
                //         InterviewSessions\
                // -------------------------------------------------

                DirectoryInfo aiFolderInfo =
                    new DirectoryInfo(
                        aiInterviewAssistantFolder);


                string mainFolder =
                    aiFolderInfo.Parent?.FullName
                    ?? aiInterviewAssistantFolder;


                // -------------------------------------------------
                // InterviewSessions root
                // -------------------------------------------------

                string interviewSessionsFolder =
                    Path.Combine(
                        mainFolder,
                        "InterviewSessions");


                // -------------------------------------------------
                // Date folder
                //
                // Example:
                //
                // 14-09-2026
                // -------------------------------------------------

                string dateFolder =
                    DateTime.Now.ToString("dd-MM-yyyy");


                string sessionFolder =
                    Path.Combine(
                        interviewSessionsFolder,
                        dateFolder);


                Directory.CreateDirectory(
                    sessionFolder);


                // -------------------------------------------------
                // Session file name
                //
                // Example:
                //
                // Interview_14-09-2026_21-13.html
                // -------------------------------------------------

                string timestamp =
                    DateTime.Now.ToString(
                        "dd-MM-yyyy_HH-mm");


                string sessionFilePath =
                    Path.Combine(
                        sessionFolder,
                        $"Interview_{timestamp}.html");


                // -------------------------------------------------
                // Prevent overwrite
                //
                // Same minute:
                //
                // Interview_14-09-2026_21-13.html
                // Interview_14-09-2026_21-13_01.html
                // Interview_14-09-2026_21-13_02.html
                // -------------------------------------------------

                int suffix = 1;

                while (File.Exists(sessionFilePath))
                {
                    sessionFilePath =
                        Path.Combine(
                            sessionFolder,
                            $"Interview_{timestamp}_{suffix:00}.html");

                    suffix++;
                }


                _sessionFilePath =
                    sessionFilePath;


                // -------------------------------------------------
                // Session start time
                //
                // Only HH:mm
                // -------------------------------------------------

                string startTime =
                    DateTime.Now.ToString("HH:mm");


                // -------------------------------------------------
                // Build initial HTML
                //
                // Keep:
                //
                // {{QA_CONTENT}}
                // {{END_TIME}}
                //
                // in memory until required.
                // -------------------------------------------------

                _sessionHtml =
                    sessionTemplate
                        .Replace(
                            "{{CSS_CONTENT}}",
                            css)
                        .Replace(
                            "{{START_TIME}}",
                            startTime);


                // -------------------------------------------------
                // Create initial session file
                // -------------------------------------------------

                SaveSessionFile();


                Debug.WriteLine(
                    $"Interview session started: {_sessionFilePath}");
            }
            catch (Exception ex)
            {
                // -------------------------------------------------
                // Logger must never crash the application.
                // -------------------------------------------------

                Debug.WriteLine(
                    $"InterviewSessionLogger initialization failed: {ex}");

                _sessionFilePath = string.Empty;
                _questionTemplate = string.Empty;
                _sessionHtml = string.Empty;
            }
        }


        // =========================================================
        // LOG QUESTION + ANSWER
        // =========================================================

        public void LogQuestionAnswer(
            string question,
            string answer)
        {
            // -----------------------------------------------------
            // Validate input
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(question))
                return;

            if (string.IsNullOrWhiteSpace(answer))
                return;


            // -----------------------------------------------------
            // Logger initialization failed
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(_sessionFilePath))
                return;


            lock (_sync)
            {
                // -------------------------------------------------
                // Do not log after session has ended.
                // -------------------------------------------------

                if (_sessionEnded)
                    return;


                try
                {
                    // -------------------------------------------------
                    // Increment question number
                    // -------------------------------------------------

                    _questionNumber++;


                    // -------------------------------------------------
                    // Encode question and answer
                    // -------------------------------------------------

                    string encodedQuestion =
                        WebUtility.HtmlEncode(
                            question.Trim());


                    string encodedAnswer =
                        WebUtility.HtmlEncode(
                            answer.Trim());


                    // -------------------------------------------------
                    // Create Q&A from separate template.
                    //
                    // No HTML markup is hard-coded here.
                    // -------------------------------------------------

                    string questionHtml =
                        _questionTemplate
                            .Replace(
                                "{{QUESTION_NUMBER}}",
                                _questionNumber.ToString())
                            .Replace(
                                "{{TIMESTAMP}}",
                                DateTime.Now.ToString("HH:mm"))
                            .Replace(
                                "{{QUESTION}}",
                                encodedQuestion)
                            .Replace(
                                "{{ANSWER}}",
                                encodedAnswer);


                    // -------------------------------------------------
                    // Add Q&A to in-memory HTML.
                    //
                    // {{QA_CONTENT}} remains available for the
                    // next question.
                    // -------------------------------------------------

                    _sessionHtml =
                        _sessionHtml.Replace(
                            "{{QA_CONTENT}}",
                            questionHtml
                            + Environment.NewLine
                            + "{{QA_CONTENT}}");


                    // -------------------------------------------------
                    // Save complete session.
                    //
                    // File.WriteAllText automatically recreates the
                    // file if it does not exist.
                    // -------------------------------------------------

                    SaveSessionFile();


                    Debug.WriteLine(
                        $"Interview Q&A logged. Question #{_questionNumber}");
                }
                catch (Exception ex)
                {
                    // -------------------------------------------------
                    // Logging failure must not crash application.
                    // -------------------------------------------------

                    Debug.WriteLine(
                        $"Interview session logging failed: {ex}");
                }
            }
        }


        // =========================================================
        // END SESSION
        // =========================================================

        public void EndSession()
        {
            // -----------------------------------------------------
            // No session file/path
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(_sessionFilePath))
                return;


            lock (_sync)
            {
                // -------------------------------------------------
                // Prevent duplicate EndSession calls.
                // -------------------------------------------------

                if (_sessionEnded)
                    return;


                _sessionEnded = true;


                try
                {
                    // -------------------------------------------------
                    // Remove remaining QA placeholder.
                    // -------------------------------------------------

                    _sessionHtml =
                        _sessionHtml.Replace(
                            "{{QA_CONTENT}}",
                            string.Empty);


                    // -------------------------------------------------
                    // End time
                    //
                    // Only HH:mm
                    // -------------------------------------------------

                    string endTime =
                        DateTime.Now.ToString("HH:mm");


                    // -------------------------------------------------
                    // Replace END_TIME
                    // -------------------------------------------------

                    _sessionHtml =
                        _sessionHtml.Replace(
                            "{{END_TIME}}",
                            endTime);


                    // -------------------------------------------------
                    // Save final session.
                    // -------------------------------------------------

                    SaveSessionFile();


                    Debug.WriteLine(
                        $"Interview session ended: {_sessionFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"Interview session end logging failed: {ex}");
                }
            }
        }


        // =========================================================
        // SAVE SESSION FILE
        // =========================================================

        private void SaveSessionFile()
        {
            if (string.IsNullOrWhiteSpace(_sessionFilePath))
                return;


            if (string.IsNullOrWhiteSpace(_sessionHtml))
                return;


            // -----------------------------------------------------
            // Make sure parent folder exists.
            // -----------------------------------------------------

            string directory =
                Path.GetDirectoryName(
                    _sessionFilePath);


            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }


            // -----------------------------------------------------
            // Write the complete HTML.
            //
            // If the file was deleted for any reason, this creates
            // it again.
            // -----------------------------------------------------

            File.WriteAllText(
                _sessionFilePath,
                _sessionHtml,
                new UTF8Encoding(false));
        }


        // =========================================================
        // FIND AiInterviewAssistant FOLDER
        // =========================================================

        private static string FindAiInterviewAssistantFolder()
        {
            // -----------------------------------------------------
            // Start from executable directory.
            //
            // Example:
            //
            // C:\Invisible app\
            //     AiInterviewAssistant\
            //         AiInterviewAssistant\
            //             bin\
            //                 Debug\
            // -----------------------------------------------------

            DirectoryInfo current =
                new DirectoryInfo(
                    AppDomain.CurrentDomain.BaseDirectory);


            // -----------------------------------------------------
            // Walk up until AiInterviewAssistant is found.
            // -----------------------------------------------------

            while (current != null)
            {
                if (string.Equals(
                    current.Name,
                    "AiInterviewAssistant",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return current.FullName;
                }


                current =
                    current.Parent;
            }


            // -----------------------------------------------------
            // Fallback
            // -----------------------------------------------------

            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}