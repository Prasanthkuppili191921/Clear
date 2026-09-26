// MainWindow.OnlineTests.UI.cs

using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // VISION RESULT
        // =========================================================

        public class VisionResult
        {
            public string Question { get; set; }

            public string Answer { get; set; }
        }


        // =========================================================
        // CAPTURE INTERVAL
        // =========================================================

        private int GetCaptureIntervalMilliseconds(
            string captureInterval)
        {
            if (string.IsNullOrWhiteSpace(
                    captureInterval))
            {
                return 1000;
            }

            switch (
                captureInterval.Trim())
            {
                case "Fast":
                    return 500;

                case "Normal":
                    return 1000;

                case "Slow":
                    return 2000;

                default:
                    return 1000;
            }
        }

        // =========================================================
        // VISION REQUEST LOCK
        // =========================================================

        private bool TryStartVisionRequest()
        {
            return Interlocked.CompareExchange(
                ref visionRequestRunning,
                1,
                0) == 0;
        }


        private void FinishVisionRequest()
        {
            Interlocked.Exchange(
                ref visionRequestRunning,
                0);
        }

        // =========================================================a
        // ONLINE TESTS UI FLOW
        // =========================================================

        private async void RunOnlineTest()
        {
            // Prevent overlapping Vision requests
            if (Interlocked.CompareExchange(
                    ref visionRequestRunning,
                    1,
                    0) != 0)
            {
                return;
            }

            try
            {
                await RunVisionAiFromScreenAsync();
            }
            catch (Exception ex)
            {
                DebugLog(
                    "RunOnlineTest ERROR: " +
                    ex);
            }
            finally
            {
                // Always release the lock
                Interlocked.Exchange(
                    ref visionRequestRunning,
                    0);
            }
        }


        // =========================================================
        // MAIN VISION FLOW
        // =========================================================

        private async Task RunVisionAiFromScreenAsync()
        {
            Border questionBubble = null;
            Bitmap screenshot = null;

            try
            {
                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();

                if (!settings.VisionEnabled)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        AppMessage.Show(
                            "Vision AI is disabled in Settings.",
                            "Vision AI",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });

                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    questionBubble =
                        AddUserMessage(
                            "🔍 Reading question...");
                });

                int captureDelay =
                    GetCaptureIntervalMilliseconds(
                        settings.CaptureInterval);

                if (captureDelay > 0)
                {
                    await Task.Delay(
                        captureDelay);
                }

                screenshot =
                    CaptureVisionScreen();

                if (screenshot == null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        UpdateUserMessage(
                            questionBubble,
                            "Unable to capture screen.");
                    });

                    return;
                }

                string base64Image =
                    ConvertBitmapToBase64(
                        screenshot);

                if (string.IsNullOrWhiteSpace(
                        base64Image))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        UpdateUserMessage(
                            questionBubble,
                            "Unable to process screen.");
                    });

                    return;
                }

                VisionResult result =
                    await AskVisionModelAsync(
                        base64Image);

                if (result == null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        UpdateUserMessage(
                            questionBubble,
                            "Could not read the question.");
                    });

                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    string question =
                        result.Question?.Trim();

                    if (!string.IsNullOrWhiteSpace(question) &&
                        !question.Equals(
                            "Question detected from screen",
                            StringComparison.OrdinalIgnoreCase) &&
                        question.IndexOf(
                            "User Safety:",
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        UpdateUserMessage(
                            questionBubble,
                            question);

                        // =====================================================
                        // IMPORTANT:
                        // This Vision question belongs to the AI bubble
                        // created below.
                        //
                        // Do NOT depend on _currentUserQuestionBubble here.
                        // =====================================================

                        questionBubble.Tag =
                            question;
                    }
                    else
                    {
                        UpdateUserMessage(
                            questionBubble,
                            "Could not identify a question.");

                        questionBubble.Tag =
                            "Could not identify a question.";
                    }
                });

                if (!string.IsNullOrWhiteSpace(
        result.Answer))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        latestAiText =
                            result.Answer;

                        Border aiBubble =
                            AddAIMessage("");

                        // =====================================================
                        // IMPORTANT:
                        // Vision flow uses its own questionBubble.
                        //
                        // Therefore explicitly associate this AI bubble
                        // with the completed Vision question.
                        // =====================================================

                        string visionQuestion =
                            questionBubble?.Tag as string;

                        if (string.IsNullOrWhiteSpace(
                                visionQuestion))
                        {
                            visionQuestion =
                                result.Question?.Trim();
                        }

                        aiBubble.Tag =
                            visionQuestion ?? string.Empty;

                        // =====================================================
                        // START AI TYPING
                        //
                        // RecordCompletedAIMessage() will be called only
                        // after the complete answer reaches the bubble.
                        // =====================================================

                        StartAITypingAnimation(
                            aiBubble,
                            result.Answer,
                            () =>
                            {
                                RecordCompletedAIMessage(
                                    aiBubble,
                                    result.Answer);
                            });
                    });
                }
            }
            catch (TaskCanceledException)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (questionBubble != null)
                    {
                        UpdateUserMessage(
                            questionBubble,
                            "Vision AI request timed out. " +
                            "Please try again.");
                    }
                });
            }
            catch (HttpRequestException ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (questionBubble != null)
                    {
                        UpdateUserMessage(
                            questionBubble,
                            "Network error while contacting Vision AI.");
                    }

                    AppMessage.Show(
                        "Vision AI network error:\n\n" +
                        ex.Message,
                        "Vision AI",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (questionBubble != null)
                    {
                        UpdateUserMessage(
                            questionBubble,
                            "Vision AI error.");
                    }

                    AppMessage.Show(
                        "Vision AI error:\n\n" +
                        ex.Message,
                        "Vision AI",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                if (screenshot != null)
                {
                    try
                    {
                        screenshot.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}