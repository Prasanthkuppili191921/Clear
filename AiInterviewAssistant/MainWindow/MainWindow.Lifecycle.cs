using System;
using System.Windows.Interop;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // WINDOW CLOSED
        // =========================================================

        private void MainWindow_Closed(
    object sender,
    EventArgs e)
        {
            try
            {
                // =========================================================
                // WINDOW MOVEMENT
                // =========================================================

                StopWindowMoveAnimation();


                // =========================================================
                // CHAT SCROLLING
                // =========================================================

                CleanupSmoothScrolling();


                // =========================================================
                // VOICE
                // =========================================================

                StopVoiceRecording();


                // =========================================================
                // AI TYPING TIMER
                // =========================================================

                if (aiTypingTimer != null)
                {
                    aiTypingTimer.Stop();

                    aiTypingTimer = null;
                }


                // =========================================================
                // CANCELLATION
                // =========================================================

                if (cancellationTokenSource != null)
                {
                    try
                    {
                        cancellationTokenSource.Cancel();
                    }
                    catch
                    {
                    }


                    try
                    {
                        cancellationTokenSource.Dispose();
                    }
                    catch
                    {
                    }


                    cancellationTokenSource = null;
                }


                // =========================================================
                // GLOBAL HOTKEYS
                // =========================================================

                HotKeysRegister.Unregister();


                // =========================================================
                // OCR
                // =========================================================

                if (ocrEngine != null)
                {
                    try
                    {
                        ocrEngine.Dispose();
                    }
                    catch
                    {
                    }

                    ocrEngine = null;
                }
            }
            catch
            {
            }
        }
    }
}