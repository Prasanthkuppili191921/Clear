using AiInterviewAssistant.Settings.Resume;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        private Action aiTypingCompletedCallback;

        //private async Task UpdateAIMessageOnUI(
        //    Border bubble,
        //    string message)
        //{
        //    await Dispatcher.InvokeAsync(() =>
        //    {
        //        UpdateAIMessage(
        //            bubble,
        //            message);
        //    });
        //}


        // =========================================================
        // AI TYPING ANIMATION
        // =========================================================

        private void StartAITypingAnimation(
            Border bubble,
            string initialText,
            Action onCompleted = null)
        {
            if (bubble == null)
                return;

            if (aiTypingTimer != null)
                aiTypingTimer.Stop();

            aiTypingCompletedCallback =
                onCompleted;

            aiTypingBubble =
                bubble;

            aiTargetText =
                initialText ?? "";

            aiDisplayedLength =
                0;


            int interval =
                5;


            try
            {
                AppSettings settings =
                    SettingsService.Load();

                if (settings != null &&
                    !string.IsNullOrWhiteSpace(
                        settings.AnimationSpeed))
                {
                    if (settings.AnimationSpeed.Equals(
                            "Fast",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        interval = 8;
                    }
                    else if (
                        settings.AnimationSpeed.Equals(
                            "Slow",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        interval = 15;
                    }
                }
            }
            catch
            {
            }


            aiTypingTimer =
                new System.Windows.Threading.DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            interval)
                };


            aiTypingTimer.Tick +=
                AITypingTimer_Tick;

            aiTypingTimer.Start();
        }


        // =========================================================
        // TYPING TIMER
        // =========================================================

        private void AITypingTimer_Tick(
    object sender,
    EventArgs e)
        {
            if (aiTypingBubble == null)
            {
                if (aiTypingTimer != null)
                    aiTypingTimer.Stop();

                return;
            }

            if (aiDisplayedLength >=
                aiTargetText.Length)
            {
                return;
            }

            int charactersToAdd =
                2;

            aiDisplayedLength =
                Math.Min(
                    aiDisplayedLength +
                    charactersToAdd,
                    aiTargetText.Length);

            string visibleText =
                aiTargetText.Substring(
                    0,
                    aiDisplayedLength);

            UpdateAIMessage(
                aiTypingBubble,
                visibleText);

            // =====================================================
            // AI BUBBLE IS NOW COMPLETELY WRITTEN
            //
            // This is important for Vision AI because Vision flow
            // uses StartAITypingAnimation() directly.
            //
            // Do NOT record before this point.
            // =====================================================

            if (aiDisplayedLength >=
                    aiTargetText.Length)
            {
                if (aiTypingTimer != null)
                    aiTypingTimer.Stop();

                aiTypingTimer = null;

                // =====================================================
                // SAVE CALLBACK BEFORE CLEARING TYPING STATE
                // =====================================================

                Action completedCallback =
                    aiTypingCompletedCallback;

                aiTypingCompletedCallback =
                    null;

                aiTypingBubble =
                    null;

                // =====================================================
                // COMPLETE AI RESPONSE IS NOW FULLY WRITTEN
                // =====================================================

                completedCallback?.Invoke();
            }
        }


        // =========================================================
        // STOP TYPING ANIMATION
        // =========================================================

        private void StopAITypingAnimation(
    Border bubble,
    string finalText)
        {
            if (bubble == null)
                return;

            if (aiTypingTimer != null)
                aiTypingTimer.Stop();

            aiTypingTimer = null;

            aiTypingBubble = null;

            aiTargetText =
                finalText ?? "";

            aiDisplayedLength =
                aiTargetText.Length;

            // =====================================================
            // FIRST:
            // COMPLETE AI BUBBLE
            //
            // This is the ONLY point where a successful AI answer
            // is considered final.
            // =====================================================

            UpdateAIMessage(
                bubble,
                aiTargetText);
        }

        // =========================================================
        // GLOBAL INTERVIEW RECORDING
        //
        // This method is intentionally independent of Voice.
        //
        // Voice ON/OFF does NOT control this.
        // Question Queue does NOT control this.
        // AI cancellation does NOT control this.
        //
        // Any completed AI bubble can use this.
        // =========================================================

        private void RecordCompletedAIMessage(
            Border bubble,
            string finalText)
        {
            try
            {
                if (bubble == null)
                    return;

                if (string.IsNullOrWhiteSpace(finalText))
                    return;

                if (_interviewSessionLogger == null)
                    return;

                // =====================================================
                // RECORD INTERVIEW CHECK
                // =====================================================

                if (!_recordInterview)
                    return;

                // =====================================================
                // QUESTION WAS CAPTURED WHEN AI BUBBLE WAS CREATED
                // =====================================================

                string question =
                    bubble.Tag as string;

                if (string.IsNullOrWhiteSpace(question))
                {
                    Debug.WriteLine(
                        "INTERVIEW RECORD: Question not available for AI bubble.");

                    return;
                }

                // =====================================================
                // FINAL QUESTION + FINAL ANSWER
                // =====================================================

                _interviewSessionLogger.LogQuestionAnswer(
                    question,
                    finalText);

                Debug.WriteLine(
                    "INTERVIEW RECORD: Completed AI response logged.");
            }
            catch (Exception ex)
            {
                // Recording must NEVER affect AI/UI functionality.
                Debug.WriteLine(
                    "INTERVIEW RECORD ERROR: " +
                    ex);
            }
        }


        // =========================================================
        // GLOBAL FINAL AI MESSAGE
        //
        // Used for terminal AI states such as errors.
        // =========================================================

        private async Task FinalizeAIMessageOnUI(
            Border bubble,
            string finalText)
        {
            if (bubble == null)
                return;

            if (string.IsNullOrWhiteSpace(finalText))
                return;

            await Dispatcher.InvokeAsync(
                () =>
                {
                    // -------------------------------------------------
                    // COMPLETE ERROR / FINAL MESSAGE IN BUBBLE
                    // -------------------------------------------------

                    UpdateAIMessage(
                        bubble,
                        finalText);

                });
        }


        // =========================================================
        // LANGUAGE INSTRUCTION
        // =========================================================

    }
}
