namespace AiInterviewAssistant.Settings.General
{
    public class GeneralBusiness
    {
        private readonly AppSettings settings;

        public GeneralBusiness(AppSettings sharedSettings)
        {
            settings =
                sharedSettings ?? new AppSettings();
        }

        // =====================================================
        // LOAD
        // =====================================================

        public string GetAnswerMode()
        {
            return string.IsNullOrWhiteSpace(settings.AnswerMode)
                ? "Short"
                : settings.AnswerMode;
        }

        // =====================================================
        // UPDATE
        // =====================================================

        public void SetAnswerMode(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                settings.AnswerMode = value;
        }

        // =====================================================
        // SAVE
        // =====================================================

        public void Save()
        {
            SettingsService.Save(settings);
        }

        // =====================================================
        // SMART ANSWER - LOAD
        // =====================================================

        public bool GetSmartAnswerEnabled()
        {
            return settings.SmartAnswerEnabled;
        }


        // =====================================================
        // SMART ANSWER - UPDATE
        // =====================================================

        public void SetSmartAnswerEnabled(
            bool value)
        {
            settings.SmartAnswerEnabled =
                value;
        }
    }
}