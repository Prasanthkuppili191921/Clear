using System.Configuration;
using AiInterviewAssistant.Security;
using Newtonsoft.Json;

namespace AiInterviewAssistant
{
    public class AppSettings
    {
        // =========================================================
        // GENERAL
        // =========================================================

        public string AnswerMode { get; set; } = "Short";

        public string Language { get; set; } = "English";

        public bool AutoClearChat { get; set; } = false;

        public string DeepgramApiKey { get; set; } = "";

        public bool SmartAnswerEnabled { get; set; } = false;



        // =========================================================
        // AI
        // =========================================================

        //public string OpenRouterApiKey
        //{
        //    get
        //    {
        //        return ConfigurationManager
        //            .AppSettings["OpenRouterKey"];
        //    }
        //}

        public string OpenRouterApiKey
        {
            get
            {
                return OpenRouterKeyProtection.GetKey();
            }
        }

        [JsonIgnore]
        public string AnswerModel { get; set; } = "";

        public double Temperature { get; set; } = 0.2;

        public string ResponseLength { get; set; } = "Medium";

        public string SystemPrompt { get; set; } = "";


        // =========================================================
        // VISION
        // =========================================================

        public bool VisionEnabled { get; set; } = true;

        [JsonIgnore]    
        public string OnlineTestModel { get; set; } = "";

        [JsonIgnore]
        public string SpeechToTextModel { get; set; } = "";

        public string CaptureInterval { get; set; } = "Fast";

        public string QuestionDetection { get; set; } = "Auto";


        // =========================================================
        // VOICE
        // =========================================================

        public bool VoiceEnabled { get; set; } = true;

        public string Microphone { get; set; } = "";

        public string OutputDevice { get; set; } = "";

        // =========================================================
        // HOTKEYS
        // =========================================================

        public string MessageHotkey { get; set; } =
            "Ctrl + Enter";

        public string VoiceHotkey { get; set; } =
            "Ctrl + Shift + V";

        public string VisionHotkey { get; set; } =
            "Alt + Enter";

       
        // =========================================================
        // APPEARANCE
        // =========================================================

        public string Theme { get; set; } = "Dark";

        public double Opacity { get; set; } = 0.85;

        public string AnimationSpeed { get; set; } =
            "Slow";


        // =========================================================
        // ADVANCED
        // =========================================================

        public int AiTimeout { get; set; } = 30;

        public int RetryCount { get; set; } = 2;

        public bool DebugLogging { get; set; } = false;

        public string ResumeText { get; set; } = string.Empty;
    }
}