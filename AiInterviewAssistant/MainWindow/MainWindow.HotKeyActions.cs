using System;
using System.Windows;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // MAIN WINDOW HOTKEY ACTION
        // =========================================================

        private void ToggleMainWindowFromHotkey()
        {
            try
            {
                if (
                    settingsManager != null &&
                    settingsManager.IsSettingsVisible)
                {
                    return;
                }


                // =========================================================
                // HIDE
                // =========================================================

                if (IsVisible)
                {
                    Hide();

                    StopWindowMoveAnimation();

                    return;
                }


                // =========================================================
                // SHOW
                // =========================================================

                WindowState =
                    WindowState.Normal;

                Show();


                // =========================================================
                // ALWAYS KEEP MAIN WINDOW ABOVE OTHER WINDOWS
                // =========================================================

                Topmost = true;

                Activate();

                Focus();
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Main window hotkey error:\n\n" +
                    ex.Message);
            }
        }

    }
}