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

        private readonly object _sync =
            new object();

        private string _sessionFilePath;

        private readonly string _questionTemplate;

        private readonly string _interviewSessionsFolder;

        private readonly Func<bool> _isRecordingEnabled;

        // Complete HTML is maintained in memory during the session.
        private string _sessionHtml;

        private bool _sessionEnded;

        private int _questionNumber;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public InterviewSessionLogger(
            Func<bool> isRecordingEnabled)
        {
            _isRecordingEnabled =
               isRecordingEnabled
               ?? (() => false);

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
                // Store InterviewSessions path only.
                //
                // IMPORTANT:
                //
                // DO NOT create the folder here.
                // -------------------------------------------------

                _interviewSessionsFolder =
                    Path.Combine(
                        mainFolder,
                        "InterviewSessions");


                // -------------------------------------------------
                // Session file does NOT exist yet.
                //
                // It will be created only when the first valid
                // AI response is logged.
                // -------------------------------------------------

                _sessionFilePath =
                    string.Empty;


                // -------------------------------------------------
                // Prepare initial HTML in memory.
                //
                // No disk write happens here.
                // -------------------------------------------------

                string startTime =
                    DateTime.Now.ToString("HH:mm");


                _sessionHtml =
                    sessionTemplate
                        .Replace(
                            "{{CSS_CONTENT}}",
                            css)
                        .Replace(
                            "{{START_TIME}}",
                            startTime);


                Debug.WriteLine(
                    "Interview session logger initialized. " +
                    "No report file created yet.");
            }
            catch (Exception ex)
            {
                // -------------------------------------------------
                // Logger must never crash the application.
                // -------------------------------------------------

                Debug.WriteLine(
                    $"InterviewSessionLogger initialization failed: {ex}");

                _sessionFilePath =
                    string.Empty;

                _questionTemplate =
                    string.Empty;

                _interviewSessionsFolder =
                    string.Empty;

                _sessionHtml =
                    string.Empty;
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

            if (!IsInterviewRecordingEnabled())
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
                    // FIRST AI RESPONSE
                    //
                    // Create folder/file only now.
                    // -------------------------------------------------

                    if (string.IsNullOrWhiteSpace(
                        _sessionFilePath))
                    {
                        CreateSessionFilePath();
                    }


                    // -------------------------------------------------
                    // If session file could not be initialized,
                    // do not continue.
                    // -------------------------------------------------

                    if (string.IsNullOrWhiteSpace(
                        _sessionFilePath))
                    {
                        return;
                    }


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
                    // Save immediately.
                    //
                    // This protects the session if the application
                    // suddenly closes after this response.
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

        private bool IsInterviewRecordingEnabled()
        {
            try
            {
                return _isRecordingEnabled();
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // CREATE SESSION FILE
        // =========================================================

        private void CreateSessionFilePath()
        {
            // -----------------------------------------------------
            // Already created
            // -----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                _sessionFilePath))
            {
                return;
            }


            // -----------------------------------------------------
            // Validate root path
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                _interviewSessionsFolder))
            {
                return;
            }


            // -----------------------------------------------------
            // Today's date folder
            //
            // Example:
            //
            // 15-09-2026
            // -----------------------------------------------------

            string dateFolder =
                DateTime.Now.ToString(
                    "dd-MM-yyyy");


            string sessionFolder =
                Path.Combine(
                    _interviewSessionsFolder,
                    dateFolder);


            // -----------------------------------------------------
            // Create folder only now.
            // -----------------------------------------------------

            Directory.CreateDirectory(
                sessionFolder);


            // -----------------------------------------------------
            // Session file name
            //
            // Example:
            //
            // Interview_15-09-2026_13-25.html
            // -----------------------------------------------------

            string timestamp =
                DateTime.Now.ToString(
                    "dd-MM-yyyy_HH-mm");


            string sessionFilePath =
                Path.Combine(
                    sessionFolder,
                    $"Interview_{timestamp}.html");


            // -----------------------------------------------------
            // Prevent overwrite when multiple sessions start
            // within the same minute.
            //
            // Example:
            //
            // Interview_15-09-2026_13-25.html
            // Interview_15-09-2026_13-25_01.html
            // Interview_15-09-2026_13-25_02.html
            // -----------------------------------------------------

            int suffix = 1;


            while (File.Exists(
                sessionFilePath))
            {
                sessionFilePath =
                    Path.Combine(
                        sessionFolder,
                        $"Interview_{timestamp}_{suffix:00}.html");

                suffix++;
            }


            _sessionFilePath =
                sessionFilePath;


            Debug.WriteLine(
                $"Interview session file created: {_sessionFilePath}");
        }


        // =========================================================
        // END SESSION
        // =========================================================

        public void EndSession()
        {
            // -----------------------------------------------------
            // IMPORTANT:
            //
            // If no AI response was logged, there is no session
            // file. Therefore do nothing.
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                _sessionFilePath))
            {
                return;
            }


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
                    // If file was removed externally, do not recreate
                    // an empty report during application shutdown.
                    // -------------------------------------------------

                    if (!File.Exists(
                        _sessionFilePath))
                    {
                        Debug.WriteLine(
                            "Interview session file no longer exists. " +
                            "Skipping EndSession save.");

                        return;
                    }


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
            // -----------------------------------------------------
            // No session yet
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                _sessionFilePath))
            {
                return;
            }


            if (string.IsNullOrWhiteSpace(
                _sessionHtml))
            {
                return;
            }


            // -----------------------------------------------------
            // Make sure parent folder exists.
            //
            // Normally it already exists because
            // CreateSessionFilePath() created it.
            // -----------------------------------------------------

            string directory =
                Path.GetDirectoryName(
                    _sessionFilePath);


            if (!string.IsNullOrWhiteSpace(
                directory))
            {
                Directory.CreateDirectory(
                    directory);
            }


            // -----------------------------------------------------
            // Write complete HTML.
            //
            // Every successful AI response gets immediately
            // persisted to disk.
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