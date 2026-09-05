using System;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // REFRESH APPEARANCE
        // =========================================================

        public void RefreshAppearanceSettings()
        {
            try
            {
                currentSettings =
                    SettingsService.Load()
                    ?? new AppSettings();

                AppearanceManager.Apply(
                    this,
                    currentSettings);
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Appearance refresh error:\n\n" +
                    ex.Message);
            }
        }
    }
}