using System.Windows;
using AiInterviewAssistant.Privacy;

namespace AiInterviewAssistant
{
    public partial class App : Application
    {
        protected override void OnStartup(
            StartupEventArgs e)
        {
            base.OnStartup(e);

            // =====================================================
            // LOAD STEALTH MODE FROM APP.CONFIG
            // =====================================================

            ScreenCaptureProtection
                .LoadStealthMode();


            // =====================================================
            // GLOBAL SCREEN CAPTURE PROTECTION
            // =====================================================

            ScreenCaptureProtection
                .RegisterGlobalWindowProtection();


            // =====================================================
            // CREATE MAIN WINDOW
            // =====================================================

            MainWindow mainWindow =
                new MainWindow();

            mainWindow.Show();

            // =====================================================
            // PROTECT ALL CURRENT WINDOWS
            // =====================================================

            ScreenCaptureProtection
                .ApplyToAllWindows(
                    ScreenCaptureProtection.IsEnabled);
        }
    }
}
