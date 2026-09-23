// MainWindow.Voice.UI.cs

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        private DispatcherTimer liveVoiceStatusTimer;

        private Grid liveVoiceAnimationGrid;

        private string liveVoiceStatus =
            "Listening";

        private int liveVoiceAnimationFrame = 0;

        private readonly Random liveVoiceRandom =
            new Random();

        private readonly int imageWidth = 96, imageHeight = 48;

        private void CreateLiveVoiceMessage()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(CreateLiveVoiceMessage));

                return;
            }

            try
            {
                RemoveLiveVoiceMessage();

                if (ChatPanel == null)
                    return;

                EnsureChatBottomSpacer();

                liveVoiceMessageBorder =
                    new Border
                    {
                        Background =
                            Brushes.Transparent,

                        BorderBrush = null,

                        BorderThickness =
                            new Thickness(0),

                        Padding =
                            new Thickness(0),

                        Margin =
                            new Thickness(0),

                        HorizontalAlignment =
                            HorizontalAlignment.Right
                    };

                liveVoiceAnimationGrid =
                    new Grid
                    {
                        Width = imageWidth,
                        Height = imageHeight,

                        HorizontalAlignment =
                            HorizontalAlignment.Center,

                        VerticalAlignment =
                            VerticalAlignment.Center
                    };

                liveVoiceMessageBorder.Child =
                    liveVoiceAnimationGrid;

                CreateVoiceStateVisual();

                if (_chatBottomSpacer != null)
                {
                    int spacerIndex =
                        ChatPanel.Children.IndexOf(
                            _chatBottomSpacer);

                    if (spacerIndex >= 0)
                    {
                        ChatPanel.Children.Insert(
                            spacerIndex,
                            liveVoiceMessageBorder);

                        ScrollLiveVoiceMessageIntoView();
                    }
                    else
                    {
                        ChatPanel.Children.Add(
                            liveVoiceMessageBorder);
                    }
                }
                else
                {
                    ChatPanel.Children.Add(
                        liveVoiceMessageBorder);
                }

                StartLiveVoiceStatusAnimation();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "CreateLiveVoiceMessage ERROR: " +
                    ex.Message);
            }
        }

        // ============================================================
        // VOICE STATE IMAGE
        //
        // Listening -> VoiceListening.png
        // STT       -> VoiceWriting.png
        //
        // VoiceAI.png is NOT loaded here.
        // It is loaded only by ShowVoiceAIVisual()
        // when AI generation actually starts.
        //
        // Images are displayed at 48 x 24.
        // ============================================================

        private void CreateVoiceStateVisual()
        {
            if (liveVoiceAnimationGrid == null)
                return;

            liveVoiceAnimationGrid.Children.Clear();

            string imageName;

            if (liveVoiceStatus == "Listening")
            {
                imageName =
                    "VoiceListening.png";
            }
            else if (liveVoiceStatus == "STT")
            {
                imageName =
                    "VoiceWriting.png";
            }
            else
            {
                return;
            }

            try
            {
                Image image =
                    new Image
                    {
                        Width = imageWidth,
                        Height = imageHeight,

                        /*
                         * Keep the complete image visible.
                         * This prevents distortion.
                         */
                        Stretch =
                            Stretch.Uniform,

                        HorizontalAlignment =
                            HorizontalAlignment.Center,

                        VerticalAlignment =
                            VerticalAlignment.Center,

                        Source =
                            new BitmapImage(
                                new Uri(
                                    "pack://application:,,,/Assets/" +
                                    imageName,
                                    UriKind.Absolute))
                    };

                liveVoiceAnimationGrid.Children.Add(
                    image);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "CreateVoiceStateVisual ERROR: " +
                    ex.Message);
            }
        }

        // ============================================================
        // AI STATE IMAGE
        //
        // VoiceAI.png is loaded ONLY when AI generation starts.
        // ============================================================

        private void ShowVoiceAIVisual()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(
                        ShowVoiceAIVisual));

                return;
            }

            if (liveVoiceAnimationGrid == null)
                return;

            try
            {
                liveVoiceStatus = "AI";

                liveVoiceAnimationGrid.Children.Clear();

                Image image =
                    new Image
                    {
                        Width = imageWidth,
                        Height = imageHeight,

                        Stretch =
                            Stretch.Uniform,

                        HorizontalAlignment =
                            HorizontalAlignment.Center,

                        VerticalAlignment =
                            VerticalAlignment.Center,

                        Source =
                            new BitmapImage(
                                new Uri(
                                    "pack://application:,,,/Assets/VoiceAI.png",
                                    UriKind.Absolute))
                    };

                liveVoiceAnimationGrid.Children.Add(
                    image);

                if (liveVoiceStatusTimer == null)
                {
                    StartLiveVoiceStatusAnimation();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "ShowVoiceAIVisual ERROR: " +
                    ex.Message);
            }
        }

        // ============================================================
        // IMAGE ANIMATION
        // ============================================================

        private void StartLiveVoiceStatusAnimation()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(
                        StartLiveVoiceStatusAnimation));

                return;
            }

            StopLiveVoiceStatusAnimation();

            liveVoiceAnimationFrame = 0;

            liveVoiceStatusTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(100)
                };

            liveVoiceStatusTimer.Tick +=
                LiveVoiceStatusTimer_Tick;

            liveVoiceStatusTimer.Start();
        }

        private void LiveVoiceStatusTimer_Tick(
            object sender,
            EventArgs e)
        {
            if (liveVoiceAnimationGrid == null)
                return;

            liveVoiceAnimationFrame++;

            if (liveVoiceAnimationGrid.Children.Count == 0)
                return;

            if (!(liveVoiceAnimationGrid.Children[0]
                is Image image))
            {
                return;
            }

            /*
             * Very subtle breathing animation.
             * The actual image size remains 48 x 24.
             */

            double scale =
                1.0 +
                (
                    Math.Sin(
                        liveVoiceAnimationFrame * 0.20)
                    * 0.025
                );

            image.RenderTransform =
                new ScaleTransform(
                    scale,
                    scale);

            image.RenderTransformOrigin =
                new Point(
                    0.5,
                    0.5);
        }

        private void StopLiveVoiceStatusAnimation()
        {
            if (liveVoiceStatusTimer != null)
            {
                liveVoiceStatusTimer.Stop();

                liveVoiceStatusTimer.Tick -=
                    LiveVoiceStatusTimer_Tick;

                liveVoiceStatusTimer = null;
            }

            if (liveVoiceAnimationGrid != null &&
                liveVoiceAnimationGrid.Children.Count > 0 &&
                liveVoiceAnimationGrid.Children[0]
                    is Image image)
            {
                image.RenderTransform = null;
            }
        }

        // ============================================================
        // SCROLL
        // ============================================================

        private void ScrollLiveVoiceMessageIntoView()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(
                        ScrollLiveVoiceMessageIntoView),
                    System.Windows.Threading.DispatcherPriority.Render);

                return;
            }

            try
            {
                if (liveVoiceMessageBorder == null ||
                    ChatScrollViewer == null ||
                    !ChatScrollViewer.IsLoaded)
                {
                    return;
                }

                ChatPanel?.UpdateLayout();

                ChatScrollViewer.UpdateLayout();

                Point point =
                    liveVoiceMessageBorder
                        .TransformToAncestor(
                            ChatScrollViewer)
                        .Transform(
                            new Point(0, 0));

                double targetOffset =
                    ChatScrollViewer.VerticalOffset +
                    point.Y;

                if (targetOffset < 0)
                    targetOffset = 0;

                if (targetOffset >
                    ChatScrollViewer.ScrollableHeight)
                {
                    targetOffset =
                        ChatScrollViewer.ScrollableHeight;
                }

                if (Math.Abs(
                        targetOffset -
                        ChatScrollViewer.VerticalOffset)
                    < 0.5)
                {
                    return;
                }

                StartSmoothChatScroll(
                    targetOffset);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "ScrollLiveVoiceMessageIntoView ERROR: " +
                    ex.Message);
            }
        }

        // ============================================================
        // UPDATE LIVE VOICE STATE
        // ============================================================

        private void UpdateLiveVoiceMessage(
            string text)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        UpdateLiveVoiceMessage(text);
                    }));

                return;
            }

            if (liveVoiceMessageBorder == null)
            {
                CreateLiveVoiceMessage();
            }

            if (liveVoiceAnimationGrid == null)
                return;

            string newStatus =
                liveVoiceStatus;

            /*
             * IMPORTANT STATE ORDER
             *
             * Listening
             *     ↓
             * STT / Writing question
             *
             * AI is NOT selected from this method.
             *
             * VoiceAI.png is shown explicitly by
             * ShowVoiceAIVisual() when AI generation starts.
             */

            if (!string.IsNullOrWhiteSpace(text) &&
                (
                    text.Contains("STT") ||
                    text.Contains("Transcribing")
                ))
            {
                newStatus =
                    "STT";
            }
            else if (!string.IsNullOrWhiteSpace(text) &&
                     text.Contains("Listening"))
            {
                newStatus =
                    "Listening";
            }

            /*
             * Change the image only when the state actually changes.
             */
            if (liveVoiceStatus != newStatus)
            {
                liveVoiceStatus =
                    newStatus;

                CreateVoiceStateVisual();
            }

            if (liveVoiceStatusTimer == null)
            {
                StartLiveVoiceStatusAnimation();
            }
        }

        // ============================================================
        // REMOVE LIVE VOICE IMAGE
        // ============================================================

        private void RemoveLiveVoiceMessage()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(
                        RemoveLiveVoiceMessage));

                return;
            }

            try
            {
                StopLiveVoiceStatusAnimation();

                if (liveVoiceMessageBorder != null &&
                    ChatPanel != null)
                {
                    ChatPanel.Children.Remove(
                        liveVoiceMessageBorder);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "RemoveLiveVoiceMessage ERROR: " +
                    ex.Message);
            }
            finally
            {
                liveVoiceMessageBorder =
                    null;

                liveVoiceMessageTextBlock =
                    null;

                liveVoiceAnimationGrid =
                    null;
            }
        }

        // ============================================================
        // LIVE VOICE TRANSCRIPT
        // ============================================================

        private void AppendLiveVoiceTranscript(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            liveVoiceTranscript +=
                string.IsNullOrWhiteSpace(
                    liveVoiceTranscript)
                    ? text.Trim()
                    : " " + text.Trim();

            /*
             * While the question is being converted/transcribed,
             * show the writing image.
             *
             * This MUST NOT switch to AI here.
             */
            if (liveVoiceStatus != "STT")
            {
                liveVoiceStatus =
                    "STT";

                CreateVoiceStateVisual();
            }
        }

        // ============================================================
        // VOICE BUTTON UI
        // ============================================================

        private void ResetVoiceButtonUI()
        {
            if (VoiceButton == null)
                return;

            VoiceButton.Background =
                null;
        }

        private void ResetVoiceUI()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(
                        ResetVoiceUI));

                return;
            }

            try
            {
                voicePulseAnimation =
                    null;

                if (VoicePulseScale != null)
                {
                    VoicePulseScale.BeginAnimation(
                        ScaleTransform.ScaleXProperty,
                        null);

                    VoicePulseScale.BeginAnimation(
                        ScaleTransform.ScaleYProperty,
                        null);
                }

                ResetVoiceButtonUI();

                SetVoiceInputMode(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "ResetVoiceUI ERROR: " +
                    ex.Message);
            }
        }

        private void SetVoiceInputMode(
            bool enabled)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        SetVoiceInputMode(enabled);
                    }));

                return;
            }

            try
            {
                if (VoiceButton == null)
                    return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SetVoiceInputMode ERROR: " +
                    ex.Message);
            }
        }
    }
}