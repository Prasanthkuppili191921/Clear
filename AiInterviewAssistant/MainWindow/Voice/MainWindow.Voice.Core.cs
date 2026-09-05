using NAudio.Wave;
using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // VOICE RECORDING STATE
        // =========================================================

        private WasapiLoopbackCapture voiceRecorder;

        private MemoryStream voiceAudioBuffer;

        private readonly object voiceAudioLock =
            new object();

        private WaveFormat voiceRecordingFormat;

        private bool voiceStopping = false;

        private bool isVoiceRecording = false;

        // =========================================================
        // LOCAL MICROPHONE RECORDING STATE
        // =========================================================

        private LocalVoiceCapture localVoiceRecorder;

        private WaveFormat localVoiceRecordingFormat;

        private bool localVoiceStopped = true;

        private readonly object localVoiceAudioLock =
            new object();


        // =========================================================
        // HTTP CLIENT
        // =========================================================

        private static readonly HttpClient
            voiceHttpClient =
            new HttpClient
            {
                Timeout =
                    TimeSpan.FromSeconds(120)
            };


        // =========================================================
        // VOICE UI STATE
        // =========================================================

        private Border liveVoiceMessageBorder;

        private TextBlock liveVoiceMessageTextBlock;

        private DoubleAnimation
            voicePulseAnimation;


        // =========================================================
        // FINAL TRANSCRIPT
        // =========================================================

        private string liveVoiceTranscript =
            string.Empty;


        // =========================================================
        // INITIALIZE STT
        // =========================================================

        private bool InitializeSpeechToText()
        {
            try
            {
                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();

                string apiKey =
                    settings.OpenRouterApiKey?.Trim();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    AppMessage.Show(
                        "OpenRouter API key is missing.\n\n" +
                        "Please configure your OpenRouter API key.");

                    return false;
                }

                string speechToTextModel =
                    settings.SpeechToTextModel?.Trim();

                if (string.IsNullOrWhiteSpace(
                    speechToTextModel))
                {
                    AppMessage.Show(
                        "Speech-to-Text model is not configured.\n\n" +
                        "Please configure the Speech-to-Text model " +
                        "from Configuration.");

                    return false;
                }

                System.Diagnostics.Debug.WriteLine(
                    "VOICE STT MODEL = " +
                    speechToTextModel);

                return true;
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Speech-to-text initialization error:\n\n" +
                    ex.Message);

                return false;
            }
        }


        // =========================================================
        // VOICE ENABLED
        // =========================================================

        private bool IsVoiceInputEnabled()
        {
            try
            {
                AppSettings settings =
                    SettingsService.Load();

                return settings?.VoiceEnabled == true;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // PUBLIC STATE
        // =========================================================

        public bool IsVoiceRecording
        {
            get
            {
                return isVoiceRecording;
            }
        }
    }
}