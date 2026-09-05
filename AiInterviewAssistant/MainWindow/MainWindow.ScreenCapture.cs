using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
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
    }
}