namespace AiInterviewAssistant.Settings.AI
{
    public class AIBusiness
    {
        private readonly AppSettings settings;

        public AIBusiness(
            AppSettings sharedSettings)
        {
            settings =
                sharedSettings ??
                new AppSettings();
        }


        // =====================================================
        // TEMPERATURE
        // =====================================================

        public double GetTemperature()
        {
            return settings.Temperature;
        }

        public void SetTemperature(
            double value)
        {
            settings.Temperature =
                value;
        }


        // =====================================================
        // RESPONSE LENGTH
        // =====================================================

        public string GetResponseLength()
        {
            return settings.ResponseLength ??
                   "Medium";
        }

        public void SetResponseLength(
            string value)
        {
            settings.ResponseLength =
                value ?? "Medium";
        }


        // =====================================================
        // SYSTEM PROMPT
        // =====================================================

        public string GetSystemPrompt()
        {
            return settings.SystemPrompt ?? "";
        }

        public void SetSystemPrompt(
            string value)
        {
            settings.SystemPrompt =
                value ?? "";
        }


        // =====================================================
        // SAVE
        // =====================================================

        public void Save()
        {
            SettingsService.Save(settings);
        }
    }
}