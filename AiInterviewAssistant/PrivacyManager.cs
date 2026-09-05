using System.Windows;

namespace AiInterviewAssistant.Privacy
{
    public class PrivacyManager
    {
        private readonly Window _window;

        public bool ScreenCaptureProtectionEnabled
        {
            get;
            private set;
        }

        public PrivacyManager(Window window)
        {
            _window = window;

            ScreenCaptureProtectionEnabled = false;
        }

        // =========================================================
        // ENABLE SCREEN CAPTURE PROTECTION
        // =========================================================

        public bool EnableScreenCaptureProtection()
        {
            if (_window == null)
                return false;

            bool result =
                ScreenCaptureProtection.Enable(_window);

            ScreenCaptureProtectionEnabled = result;

            return result;
        }

        // =========================================================
        // DISABLE SCREEN CAPTURE PROTECTION
        // =========================================================

        public bool DisableScreenCaptureProtection()
        {
            if (_window == null)
                return false;

            bool result =
                ScreenCaptureProtection.Disable(_window);

            if (result)
            {
                ScreenCaptureProtectionEnabled = false;
            }

            return result;
        }

        // =========================================================
        // APPLY
        // =========================================================

        public void Apply()
        {
            EnableScreenCaptureProtection();
        }
    }
}