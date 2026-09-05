using Newtonsoft.Json;
using System;
using System.IO;
using System.Configuration;

namespace AiInterviewAssistant
{
    public static class SettingsService
    {
        private static readonly string SettingsFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "AiInterviewAssistant");


        private static readonly string SettingsFile =
            Path.Combine(
                SettingsFolder,
                "settings.json");

        // =========================================================
        // CONFIGURATION MODELS
        // =========================================================

        private static readonly string answerModel =
            ConfigurationManager.AppSettings[
                "OpenRouter_AnswerModel"];

        private static readonly string onlineTestModel =
            ConfigurationManager.AppSettings[
                "OpenRouter_OnlineTestModel"];

        private static readonly string speechToTextModel =
            ConfigurationManager.AppSettings[
                "OpenRouter_SpeechToTextModel"];


        // =========================================================
        // LOAD
        // =========================================================

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                {
                    return CreateDefaultSettings();
                }


                string json =
                    File.ReadAllText(
                        SettingsFile);


                if (string.IsNullOrWhiteSpace(json))
                {
                    return CreateDefaultSettings();
                }


                AppSettings settings =
                    JsonConvert.DeserializeObject<AppSettings>(
                        json);


                if (settings == null)
                {
                    return CreateDefaultSettings();
                }


                // -------------------------------------------------
                // RUNTIME CONFIGURATION
                // -------------------------------------------------

                settings.AnswerModel =
                    answerModel;

                settings.OnlineTestModel =
                    onlineTestModel;

                settings.SpeechToTextModel =
                    speechToTextModel;


                return settings;
            }
            catch
            {
                return CreateDefaultSettings();
            }
        }


        // =========================================================
        // SAVE
        // =========================================================

        public static bool Save(
            AppSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    return false;
                }


                if (!Directory.Exists(
                        SettingsFolder))
                {
                    Directory.CreateDirectory(
                        SettingsFolder);
                }


                string json =
                    JsonConvert.SerializeObject(
                        settings,
                        Formatting.Indented);


                File.WriteAllText(
                    SettingsFile,
                    json);


                return true;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // RESET
        // =========================================================

        public static AppSettings Reset()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    File.Delete(SettingsFile);
                }
            }
            catch
            {
            }


            return CreateDefaultSettings();
        }


        // =========================================================
        // DEFAULT SETTINGS
        // =========================================================

        private static AppSettings CreateDefaultSettings()
        {
            return new AppSettings
            {
                // -------------------------------------------------
                // GENERAL
                // -------------------------------------------------

                AnswerMode =
                    "Auto",

                Language =
                    "English",

                AutoClearChat =
                    false,

                SmartAnswerEnabled =
                    false,

                // -------------------------------------------------
                // AI
                // -------------------------------------------------

                AnswerModel =
                    answerModel,

                Temperature =
                    0.2,

                ResponseLength =
                    "Medium",

                SystemPrompt =
                    "",


                // -------------------------------------------------
                // VISION
                // -------------------------------------------------

                VisionEnabled =
                    true,

                OnlineTestModel =
                    onlineTestModel,

                SpeechToTextModel =
                    speechToTextModel,

                CaptureInterval =
                    "Fast",

                QuestionDetection =
                    "Auto",


                // -------------------------------------------------
                // VOICE
                // -------------------------------------------------

                VoiceEnabled =
                    true,

                Microphone =
                    "",

                OutputDevice =
                    "",


                // -------------------------------------------------
                // HOTKEYS
                // -------------------------------------------------

                MessageHotkey =
                    "Ctrl + Enter",

                VoiceHotkey =
                    "Ctrl + Shift + V",

                VisionHotkey =
                    "Alt + Enter",


                // -------------------------------------------------
                // APPEARANCE
                // -------------------------------------------------

                Theme =
                    "Dark",

                Opacity =
                    0.85,

                AnimationSpeed =
                    "Slow",


                // -------------------------------------------------
                // ADVANCED
                // -------------------------------------------------

                AiTimeout =
                    30,

                RetryCount =
                    2,

                DebugLogging =
                    false,

                ResumeText =
                    string.Empty
            };
        }
    }
}