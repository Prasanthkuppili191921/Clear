// MainWindow.Voice.UI.cs

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
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

                // =========================================================
                // MAKE SURE THE CHAT SPACER EXISTS
                //
                // Normal messages are inserted BEFORE this spacer.
                // Live voice message must follow the same structure.
                // =========================================================

                EnsureChatBottomSpacer();

                liveVoiceMessageBorder =
                    new Border
                    {
                        Background =
                            new SolidColorBrush(
                                Color.FromArgb(
                                    45, 77, 141, 255)),

                        BorderBrush =
                            new SolidColorBrush(
                                Color.FromArgb(
                                    75, 77, 141, 255)),

                        BorderThickness =
                            new Thickness(1),

                        CornerRadius =
                            new CornerRadius(16),

                        Padding =
                            new Thickness(14, 10, 14, 10),

                        Margin =
                            new Thickness(50, 5, 4, 5),

                        HorizontalAlignment =
                            HorizontalAlignment.Right,

                        MaxWidth = 470
                    };

                liveVoiceMessageTextBlock =
                    new TextBlock
                    {
                        Text = "Listening...",

                        Foreground =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    244, 246, 250)),

                        FontSize = 14,

                        TextWrapping =
                            TextWrapping.Wrap
                    };

                liveVoiceMessageBorder.Child =
                    liveVoiceMessageTextBlock;

                // =========================================================
                // IMPORTANT:
                // INSERT LIVE VOICE MESSAGE BEFORE THE BOTTOM SPACER.
                //
                // Do NOT use Children.Add().
                // Do NOT scroll to end here.
                // =========================================================

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

                        // =========================================================
                        // VOICE STATUS MUST ALWAYS BE VISIBLE
                        //
                        // If the user has manually scrolled upward,
                        // bring only the live voice message into view.
                        //
                        // Do NOT ScrollToEnd().
                        // =========================================================

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
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "CreateLiveVoiceMessage ERROR: " +
                    ex.Message);
            }
        }

        private void ScrollLiveVoiceMessageIntoView()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(ScrollLiveVoiceMessageIntoView),
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

                // Make sure the newly inserted voice bubble
                // has its final layout position.
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

                // No unnecessary movement.
                if (Math.Abs(
                        targetOffset -
                        ChatScrollViewer.VerticalOffset) < 0.5)
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

            if (liveVoiceMessageTextBlock == null)
            {
                CreateLiveVoiceMessage();
            }

            if (liveVoiceMessageTextBlock == null)
                return;

            liveVoiceMessageTextBlock.Text =
                string.IsNullOrWhiteSpace(text)
                    ? "Listening..."
                    : text.Trim();

        }

        private void RemoveLiveVoiceMessage()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(RemoveLiveVoiceMessage));
                return;
            }

            try
            {
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
                liveVoiceMessageBorder = null;
                liveVoiceMessageTextBlock = null;
            }
        }

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

            UpdateLiveVoiceMessage(
                liveVoiceTranscript);
        }

        private void ResetVoiceButtonUI()
        {
            if (VoiceButton == null)
                return;

            VoiceButton.Background = null;
            VoiceButton.ToolTip =
                "Voice Input";
        }

        private void ResetVoiceUI()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(ResetVoiceUI));
                return;
            }

            try
            {
                voicePulseAnimation = null;

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

                VoiceButton.ToolTip =
                    enabled
                        ? "Stop Voice Input"
                        : "Start Voice Input";
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