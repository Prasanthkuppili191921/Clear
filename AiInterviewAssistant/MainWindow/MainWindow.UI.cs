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
using MessageBox = System.Windows.Forms.MessageBox;

namespace AiInterviewAssistant
{
    public partial class MainWindow : Window
    {
        // =========================================================
        // TEXT INPUT
        // =========================================================

        private async void QuestionTextBox_KeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;

            e.Handled = true;

            await SendQuestion();
        }


        // =========================================================
        // SEND BUTTON
        // =========================================================

        private async void SendButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                await SendQuestion();
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Send error:\n\n" + ex.Message);
            }
        }


        // =========================================================
        // STOP BUTTON
        // =========================================================

        private void StopButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        // =========================================================
        // WINDOW LOADED - TOP CENTER
        // =========================================================

        private void Window_Loaded(
    object sender,
    RoutedEventArgs e)
        {
            try
            {
                double workAreaLeft =
                    SystemParameters.WorkArea.Left;

                double workAreaTop =
                    SystemParameters.WorkArea.Top;

                double workAreaWidth =
                    SystemParameters.WorkArea.Width;

                Left =
                    workAreaLeft +
                    ((workAreaWidth - ActualWidth) / 2);

                Top =
                    workAreaTop;

                // =========================================
                // MAIN WINDOW ALWAYS ON TOP
                // =========================================

                Topmost = true;
                Activate();
            }
            catch
            {
                // Ignore positioning errors.
            }
        }


        // =========================================================
        // NEW CHAT
        // =========================================================

        private void NewChatButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (isGenerating &&
                cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }


            ClearConversation();
        }


        // =========================================================
        // MESSAGE MODE
        // =========================================================

        // =========================================================
        // MESSAGE MODE BUTTON
        // =========================================================

        private void MessageModeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                // -------------------------------------------------
                // If voice recording is active, don't switch
                // to message mode while recording.
                // -------------------------------------------------

                if (isVoiceRecording)
                {
                    return;
                }


                // -------------------------------------------------
                // TOGGLE TEXT INPUT
                // -------------------------------------------------

                if (TextInputPanel == null)
                {
                    return;
                }


                if (TextInputPanel.Visibility ==
                    Visibility.Visible)
                {
                    // =============================================
                    // SECOND CLICK
                    // HIDE TEXTBOX + SEND ICON
                    // =============================================

                    TextInputPanel.Visibility =
                        Visibility.Collapsed;


                    // Clear old text
                    if (QuestionTextBox != null)
                    {
                        QuestionTextBox.Clear();
                    }
                }
                else
                {
                    // =============================================
                    // FIRST CLICK
                    // SHOW TEXTBOX + SEND ICON
                    // =============================================

                    TextInputPanel.Visibility =
                        Visibility.Visible;


                    // Focus textbox
                    if (QuestionTextBox != null)
                    {
                        QuestionTextBox.Clear();

                        QuestionTextBox.Focus();

                        QuestionTextBox.CaretIndex =
                            QuestionTextBox.Text.Length;
                    }
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // VOICE BUTTON
        // =========================================================

        private void VoiceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
               
                // =====================================================
                // EXISTING VOICE FUNCTIONALITY
                //
                // ChatGPTView=false
                //
                // DO NOT CHANGE THIS FLOW.
                // =====================================================

                // -----------------------------------------
                // START VOICE
                // -----------------------------------------

                if (!isVoiceRecording &&
                    voiceRecorder == null &&
                    !voiceStopping)
                {
                    TextInputPanel.Visibility =
                        Visibility.Collapsed;

                    StartVoiceRecording();

                    return;
                }

                // -----------------------------------------
                // STOP VOICE
                // -----------------------------------------

                if (isVoiceRecording &&
                    voiceRecorder != null &&
                    !voiceStopping)
                {
                    StopVoiceRecording();

                    return;
                }
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Voice button error:\n\n" +
                    ex.Message);
            }
        }
    }
}