using AiInterviewAssistant.ScreenQuestion;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Tesseract;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;

namespace AiInterviewAssistant
{
    public partial class MainWindow : Window
    {
        // SCREEN CAPTURE, OCR and online-test question extraction.
        [DllImport(
    "dwmapi.dll",
    PreserveSig = true)]
        private static extern int DwmFlush();
        private Bitmap CaptureFullScreen()
        {
            Rectangle bounds =
                System.Windows.Forms.SystemInformation.VirtualScreen;

            Bitmap screenshot =
                new Bitmap(
                    bounds.Width,
                    bounds.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics graphics =
                   Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(
                    bounds.Left,
                    bounds.Top,
                    0,
                    0,
                    bounds.Size,
                    CopyPixelOperation.SourceCopy);
            }

            return screenshot;
        }

        private async Task<Bitmap> CaptureScreenWithoutAssistant()
        {
            bool wasVisible = IsVisible;

            try
            {
                if (wasVisible)
                {
                    Hide();

                    // Allow the browser underneath to repaint completely.
                    await Task.Delay(250);
                }

                return CaptureFullScreen();
            }
            finally
            {
                if (wasVisible)
                {
                    Show();
                    Activate();
                    QuestionTextBox?.Focus();
                }
            }
        }

        private async Task CaptureScreenAndRunOCR()
        {
            try
            {
                if (ocrEngine == null)
                {
                    AppMessage.Show(
                        "OCR is not initialized.");

                    return;
                }

                Bitmap screenshot =
                    CaptureFullScreen();

                _currentScreenshot?.Dispose();

                _currentScreenshot =
                    screenshot;

                string tempImagePath =
                    Path.Combine(
                        Path.GetTempPath(),
                        "ai_ocr_screen.png");

                screenshot.Save(
                    tempImagePath,
                    System.Drawing.Imaging.ImageFormat.Png);

                string extractedText =
                    await Task.Run(() =>
                    {
                        using (Tesseract.Pix pix =
                               Tesseract.Pix.LoadFromFile(
                                   tempImagePath))
                        using (Tesseract.Page page =
                               ocrEngine.Process(pix))
                        {
                            return page
                                .GetText()
                                ?.Trim();
                        }
                    });

                try
                {
                    if (File.Exists(tempImagePath))
                        File.Delete(tempImagePath);
                }
                catch
                {
                }

                if (string.IsNullOrWhiteSpace(
                        extractedText))
                {
                    QuestionTextBox.Text =
                        "No text detected.";

                    return;
                }

                QuestionTextBox.Text =
                    extractedText;

                QuestionTextBox.CaretIndex =
                    QuestionTextBox.Text.Length;

                QuestionTextBox.Focus();
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "OCR error:\n\n" +
                    ex.Message);
            }
        }

        private string ExtractMcqFromOcr(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return string.Empty;

            string normalized =
                ocrText
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");

            string[] lines =
                normalized
                    .Split('\n')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

            if (lines.Length == 0)
                return string.Empty;

            StringBuilder result =
                new StringBuilder();

            int optionCount = 0;
            bool questionStarted = false;
            bool optionsStarted = false;

            foreach (string line in lines)
            {
                string cleanedLine =
                    Regex.Replace(
                        line,
                        @"\s+",
                        " ").Trim();

                if (string.IsNullOrWhiteSpace(cleanedLine))
                    continue;

                // ---------------------------------------------------------
                // OPTION DETECTION
                // Supports:
                // A) text
                // A. text
                // A: text
                // (A) text
                // 1) text
                // 1. text
                // (1) text
                // ---------------------------------------------------------

                bool isOption =
                    Regex.IsMatch(
                        cleanedLine,
                        @"^\(?[A-Da-d]\)?[\.\):\-]\s+.+") ||

                    Regex.IsMatch(
                        cleanedLine,
                        @"^\([A-Da-d]\)\s+.+") ||

                    Regex.IsMatch(
                        cleanedLine,
                        @"^\(?[1-4]\)?[\.\):\-]\s+.+");

                if (isOption)
                {
                    optionsStarted = true;
                    optionCount++;

                    result.AppendLine(
                        cleanedLine);

                    continue;
                }

                // ---------------------------------------------------------
                // QUESTION / TEXT
                // ---------------------------------------------------------

                if (!optionsStarted)
                {
                    // Ignore obvious website/header noise
                    if (IsLikelyWebsiteNoise(cleanedLine))
                        continue;

                    questionStarted = true;

                    result.AppendLine(
                        cleanedLine);

                    continue;
                }

                // ---------------------------------------------------------
                // AFTER OPTIONS
                //
                // Ignore common footer/navigation text.
                // ---------------------------------------------------------

                if (IsLikelyWebsiteNoise(cleanedLine))
                    continue;

                // Keep continuation of an option if OCR
                // placed it on another line.
                result.AppendLine(
                    cleanedLine);
            }

            string finalText =
                result.ToString().Trim();

            // ---------------------------------------------------------
            // If we found at least one option, return filtered text.
            // ---------------------------------------------------------

            if (optionCount >= 1)
                return finalText;

            // ---------------------------------------------------------
            // If no options were detected, don't destroy the OCR.
            // Return the original cleaned text so AI can still understand.
            // ---------------------------------------------------------

            return string.Join(
                Environment.NewLine,
                lines);
        }

        private bool IsLikelyWebsiteNoise(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true;

            string text =
                line.Trim().ToLowerInvariant();

            string[] noiseWords =
            {
                "logout",
                "log out",
                "sign out",
                "dashboard",

                "home",
                "profile",
                "settings",
                "help",
                "submit test",
                "submit",
                "next question",
                "previous question",
                "time left",
                "timer",
                "mark for review",
                "review",
                "skip",
                "menu",
                "instructions"
            };

            foreach (string word in noiseWords)
            {
                if (text == word ||
                    text.Contains(word))
                {
                    return true;
                }
            }

            // Very short navigation-like text
            if (text.Length <= 2 &&
                !Regex.IsMatch(text, @"^[a-d1-4]$"))
            {
                return true;
            }

            return false;
        }

        private string ConvertScreenshotToBase64()
        {
            if (_currentScreenshot == null)
                return null;

            using (MemoryStream stream = new MemoryStream())
            {
                _currentScreenshot.Save(
                    stream,
                    System.Drawing.Imaging.ImageFormat.Jpeg);

                return Convert.ToBase64String(
                    stream.ToArray());
            }
        }

        private async Task CaptureOnlineTestQuestion()
        {
            lock (onlineCaptureLock)
            {
                if (isCapturingOnlineTestQuestion)
                    return;

                isCapturingOnlineTestQuestion = true;
            }

            try
            {
                if (ocrEngine == null)
                {
                    AppMessage.Show(
                        "OCR is not initialized.");

                    return;
                }

                // ---------------------------------------------------------
                // CAPTURE FRESH SCREEN
                // ---------------------------------------------------------

                Bitmap screenshot =
                    await CaptureScreenWithoutAssistant();

                if (screenshot == null)
                    return;

                _currentScreenshot?.Dispose();
                _currentScreenshot = screenshot;

                string tempImagePath =
                    Path.Combine(
                        Path.GetTempPath(),
                        "online_test_ocr_" +
                        Guid.NewGuid().ToString("N") +
                        ".png");

                try
                {
                    screenshot.Save(
                        tempImagePath,
                        System.Drawing.Imaging.ImageFormat.Png);

                    // ---------------------------------------------------------
                    // OCR
                    // ---------------------------------------------------------

                    string extractedText =
                        await Task.Run(() =>
                        {
                            using (Tesseract.Pix pix =
                                   Tesseract.Pix.LoadFromFile(
                                       tempImagePath))
                            using (Tesseract.Page page =
                                   ocrEngine.Process(pix))
                            {
                                return page
                                    .GetText()
                                    ?.Trim();
                            }
                        });

                    if (string.IsNullOrWhiteSpace(extractedText))
                    {
                        AppMessage.Show(
                            "No text could be detected from the screen.");

                        return;
                    }

                    // ---------------------------------------------------------
                    // DEBUG / CLEAN OCR TEXT
                    // ---------------------------------------------------------

                    string cleanedText =
                        CleanOcrText(extractedText);

                    if (string.IsNullOrWhiteSpace(cleanedText))
                    {
                        AppMessage.Show(
                            "OCR text is empty.");

                        return;
                    }

                    // ---------------------------------------------------------
                    // TRY MCQ EXTRACTION
                    // ---------------------------------------------------------

                    string mcqText =
                        ExtractMcqFromOcr(cleanedText);

                    // ---------------------------------------------------------
                    // IMPORTANT
                    //
                    // If MCQ extraction fails, DO NOT THROW AWAY OCR TEXT.
                    // Send the complete OCR text to AI.
                    // ---------------------------------------------------------

                    if (string.IsNullOrWhiteSpace(mcqText))
                    {
                        mcqText = cleanedText;
                    }

                    // ---------------------------------------------------------
                    // LIMIT EXTREME OCR NOISE
                    // ---------------------------------------------------------

                    if (mcqText.Length > 12000)
                    {
                        mcqText =
                            mcqText.Substring(0, 12000);
                    }

                    // ---------------------------------------------------------
                    // PUT QUESTION INTO TEXTBOX
                    // ---------------------------------------------------------

                    await Dispatcher.InvokeAsync(() =>
                    {
                        QuestionTextBox.Text = mcqText;

                        QuestionTextBox.CaretIndex =
                            QuestionTextBox.Text.Length;

                        QuestionTextBox.ScrollToEnd();
                    });

                    // ---------------------------------------------------------
                    // ONLINE TEST / MCQ MODE
                    // ---------------------------------------------------------

                    isOnlineTestMode = true;

                    try
                    {
                        await SendQuestion();
                    }
                    finally
                    {
                        isOnlineTestMode = false;
                    }
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempImagePath))
                            File.Delete(tempImagePath);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                isOnlineTestMode = false;

                await Dispatcher.InvokeAsync(() =>
                {
                    AppMessage.Show(
                        "Online test detection error:\n\n" +
                        ex.Message);
                });
            }
            finally
            {
                lock (onlineCaptureLock)
                {
                    isCapturingOnlineTestQuestion = false;
                }
            }
        }

        private string CleanOcrText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string[] lines =
                text.Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Split('\n');

            StringBuilder result =
                new StringBuilder();

            foreach (string rawLine in lines)
            {
                string line =
                    rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Remove obvious OCR garbage
                if (line.Length == 1 &&
                    !char.IsLetterOrDigit(line[0]))
                {
                    continue;
                }

                result.AppendLine(line);
            }

            return result
                .ToString()
                .Trim();
        }

      
        private string ConvertBitmapToBase64Jpeg(Bitmap bitmap)
        {
            if (bitmap == null)
                return null;

            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(
                    stream,
                    System.Drawing.Imaging.ImageFormat.Jpeg);

                return Convert.ToBase64String(
                    stream.ToArray());
            }
        }

        private async Task<UniversalScreenQuestionResult>
    ReadUniversalScreenQuestionAsync()
        {
            Bitmap screenshot = null;

            try
            {
                screenshot =
                 // await _universalScreenCapture.CaptureAsync();
                 await CaptureUniversalScreen();


                if (screenshot == null)
                {
                    return new UniversalScreenQuestionResult
                    {
                        IsQuestion = false,
                        IsSkillRelated = false,
                        Reason = "Unable to capture screen."
                    };
                }

                string debugPath =
                        Path.Combine(
                            Path.GetTempPath(),
                            "UniversalCapture_Debug.png");

                                    screenshot.Save(
                                        debugPath,
                                        System.Drawing.Imaging.ImageFormat.Png);

                                    Debug.WriteLine(
                                        "UNIVERSAL CAPTURE SAVED: " +
                                        debugPath);

                string imageBase64 =
                    ConvertBitmapToBase64Jpeg(screenshot);

                if (string.IsNullOrWhiteSpace(imageBase64))
                {
                    return new UniversalScreenQuestionResult
                    {
                        IsQuestion = false,
                        IsSkillRelated = false,
                        Reason = "Unable to convert screenshot."
                    };
                }

                // =====================================================
                // USE EXISTING MAIN WINDOW SETTINGS
                // =====================================================

                string apiKey =
                    currentSettings.OpenRouterApiKey;

                string model =
                    currentSettings.OnlineTestModel;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new UniversalScreenQuestionResult
                    {
                        IsQuestion = false,
                        IsSkillRelated = false,
                        Reason = "OpenRouter API key is not configured."
                    };
                }

                if (string.IsNullOrWhiteSpace(model))
                {
                    return new UniversalScreenQuestionResult
                    {
                        IsQuestion = false,
                        IsSkillRelated = false,
                        Reason = "Online Test model is not configured."
                    };
                }

                return await _universalScreenQuestionService
                    .AnalyzeScreenAsync(
                        imageBase64,
                        apiKey,
                        model,
                        CancellationToken.None);
            }
            finally
            {
                screenshot?.Dispose();
            }
        }

        private async Task<Bitmap> CaptureUniversalScreen()
        {
            try
            {
                // =============================================================
                // IMPORTANT:
                //
                // Do NOT Hide() the assistant.
                // Do NOT Show() the assistant.
                //
                // This keeps the Universal Alt+Enter flow completely
                // blink-free.
                //
                // =============================================================

                // Give the target application time to finish processing
                // the tab switch / screen update.
                await Task.Delay(300);

                // Ask Windows Desktop Window Manager to finish pending
                // composition work.
                try
                {
                    DwmFlush();
                }
                catch
                {
                }

                // Give the newly composed frame a little time to become
                // available to CopyFromScreen().
                await Task.Delay(100);

                // =============================================================
                // FIRST FRAME
                // =============================================================

                Bitmap firstFrame =
                    CaptureFullScreen();

                if (firstFrame == null)
                    return null;

                // =============================================================
                // SECOND FRAME
                // =============================================================

                await Task.Delay(100);

                try
                {
                    DwmFlush();
                }
                catch
                {
                }

                Bitmap secondFrame =
                    CaptureFullScreen();

                if (secondFrame == null)
                {
                    return firstFrame;
                }

                // First frame is no longer needed.
                firstFrame.Dispose();

                // =============================================================
                // THIRD / FINAL FRAME
                // =============================================================

                await Task.Delay(100);

                try
                {
                    DwmFlush();
                }
                catch
                {
                }

                Bitmap finalFrame =
                    CaptureFullScreen();

                if (finalFrame == null)
                {
                    return secondFrame;
                }

                // Second frame is no longer needed.
                secondFrame.Dispose();

                // =============================================================
                // IMPORTANT:
                //
                // Always return the latest frame.
                //
                // =============================================================

                return finalFrame;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "UNIVERSAL SCREEN CAPTURE ERROR:");

                Debug.WriteLine(
                    ex.ToString());

                return null;
            }
        }

        private async Task HandleUniversalAltEnterAsync()
        {
            try
            {
                // =========================================================
                // 1. PREVENT DUPLICATE UNIVERSAL REQUEST
                // =========================================================

                if (isGenerating)
                {
                    Debug.WriteLine(
                        "UNIVERSAL ALT + ENTER: AI generation already running.");

                    return;
                }


                // =========================================================
                // 2. START EXISTING VISION REQUEST GUARD
                // =========================================================

                if (!TryStartVisionRequest())
                {
                    Debug.WriteLine(
                        "UNIVERSAL ALT + ENTER: Vision request already running.");

                    return;
                }


                try
                {
                    // =====================================================
                    // 3. SHOW READING QUESTION
                    // =====================================================
                    //
                    // Same UI pattern as existing Vision flow.
                    //
                    // =====================================================

                    Border questionBubble =
                        null;


                    await Dispatcher.InvokeAsync(() =>
                    {
                        questionBubble =
                            AddUserMessage(
                                "🔍 Reading question...");
                    });


                    // =====================================================
                    // 4. READ QUESTION FROM SCREEN
                    // =====================================================

                    UniversalScreenQuestionResult result =
                        await ReadUniversalScreenQuestionAsync();


                    // =====================================================
                    // 5. VALIDATE RESULT
                    // =====================================================

                    if (result == null)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (questionBubble != null)
                            {
                                UpdateUserMessage(
                                    questionBubble,
                                    "Could not read the question.");
                            }
                        });

                        return;
                    }


                    if (!result.IsQuestion)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (questionBubble != null)
                            {
                                UpdateUserMessage(
                                    questionBubble,
                                    "Could not identify a question.");
                            }
                        });

                        return;
                    }


                    // =====================================================
                    // 6. SKILL CHECK
                    // =====================================================

                    if (!result.IsSkillRelated)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (questionBubble != null)
                            {
                                UpdateUserMessage(
                                    questionBubble,
                                    "Question is not related to the configured skills.");
                            }
                        });

                        return;
                    }


                    // =====================================================
                    // 7. BUILD QUESTION
                    // =====================================================

                    string question =
                        result.Question?.Trim();


                    if (string.IsNullOrWhiteSpace(question))
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (questionBubble != null)
                            {
                                UpdateUserMessage(
                                    questionBubble,
                                    "Could not identify a question.");
                            }
                        });

                        return;
                    }


                    // =====================================================
                    // 8. ADD MCQ OPTIONS
                    // =====================================================

                    if (!string.IsNullOrWhiteSpace(
                            result.Options))
                    {
                        question +=
                            "\n\nOptions:\n" +
                            result.Options.Trim();
                    }


                    Debug.WriteLine(
                        "=================================================");

                    Debug.WriteLine(
                        "UNIVERSAL QUESTION:");

                    Debug.WriteLine(
                        question);

                    Debug.WriteLine(
                        "UNIVERSAL QUESTION TYPE: " +
                        result.QuestionType);

                    Debug.WriteLine(
                        "=================================================");


                    // =====================================================
                    // 9. UPDATE EXISTING QUESTION BUBBLE
                    // =====================================================
                    //
                    // Do NOT remove and recreate it.
                    //
                    // Existing Vision flow updates the same bubble.
                    //
                    // =====================================================

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (questionBubble != null)
                        {
                            UpdateUserMessage(
                                questionBubble,
                                question);
                        }
                    });


                    // =====================================================
                    // 10. SEND DIRECTLY TO EXISTING AI PIPELINE
                    // =====================================================
                    //
                    // IMPORTANT:
                    //
                    // NO:
                    //     QuestionTextBox.Text = question;
                    //
                    // NO:
                    //     SendQuestion();
                    //
                    // Instead:
                    //
                    //     SendQuestion(question)
                    //
                    // This follows the exact Ctrl+Enter pipeline internally
                    // without requiring the question textbox.
                    //
                    // =====================================================

                    await SendUniversalQuestionAsync(question);
                }
                finally
                {
                    // =====================================================
                    // RELEASE VISION REQUEST GUARD
                    // =====================================================

                    FinishVisionRequest();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "UNIVERSAL ALT + ENTER ERROR:");

                Debug.WriteLine(ex.ToString());
            }
        }

        private string BuildUniversalInterviewPrompt(string question)
        {
            return $@"
                        You are an interview assistant.

                        Answer the following interview question directly and concisely.

                        QUESTION:
                        {question}

                        RULES:
                        - Give only the answer the interviewer needs.
                        - Keep the answer short and interview-ready.
                        - For normal technical questions, answer in 3-6 sentences.
                        - For comparison questions, give only the key differences.
                        - For MCQs, give the correct option first, then one short explanation.
                        - For coding questions, provide the code first, followed by a brief explanation.
                        - For SQL questions, provide the query first, followed by a brief explanation.
                        - If an example is required, give one concise example.
                        - Do not repeat the question.
                        - Do not add unnecessary introduction or conclusion.
                        - Do not provide lengthy background information.
                        - Do not over-explain.
                        - Use professional interview language.";
        }

        private async Task SendUniversalQuestionAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;

            try
            {
                string prompt =
                    BuildUniversalInterviewPrompt(question);

                // =====================================================
                // AI RESPONSE BUBBLE
                // =====================================================

                Border thinkingBubble =
                    AddAIMessage("");

                StartAITypingAnimation(
                    thinkingBubble,
                    "");

                // =====================================================
                // EXISTING OPENROUTER STREAMING
                // =====================================================

                await AskOpenRouterStreaming(
                    prompt,
                    thinkingBubble,
                    CancellationToken.None,
                    "Medium");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "UNIVERSAL AI ERROR: " +
                    ex);

                if (aiTypingBubble != null)
                {
                    StopAITypingAnimation(
                        aiTypingBubble,
                        "Error: " + ex.Message);
                }
            }
        }
    }
}