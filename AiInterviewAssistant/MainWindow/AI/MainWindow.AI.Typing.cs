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
            string initialText)
        {
            if (bubble == null)
                return;

            if (aiTypingTimer != null)
                aiTypingTimer.Stop();


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
        }


        // =========================================================
        // STOP TYPING ANIMATION
        // =========================================================

        private void StopAITypingAnimation(
            Border bubble,
            string finalText)
        {
            if (aiTypingTimer != null)
                aiTypingTimer.Stop();

            aiTypingTimer = null;

            aiTypingBubble = null;

            aiTargetText =
                finalText ?? "";

            aiDisplayedLength =
                aiTargetText.Length;


            UpdateAIMessage(
                bubble,
                aiTargetText);
        }


        // =========================================================
        // LANGUAGE INSTRUCTION
        // =========================================================

    }
}
