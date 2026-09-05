using AiInterviewAssistant;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AiInterviewAssistant.Settings.Voice
{
    public class VoiceBusiness
    {
        private readonly AppSettings settings;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public VoiceBusiness(AppSettings settings)
        {
            this.settings = settings;
        }

        // =====================================================
        // VOICE ENABLED
        // =====================================================

        public bool VoiceEnabled
        {
            get => settings.VoiceEnabled;
            set => settings.VoiceEnabled = value;
        }

        // =====================================================
        // MICROPHONE
        // =====================================================

        public string Microphone
        {
            get => settings.Microphone;
            set => settings.Microphone = value;
        }

        // =====================================================
        // OUTPUT DEVICE
        // =====================================================

        public string OutputDevice
        {
            get => settings.OutputDevice;
            set => settings.OutputDevice = value;
        }

        // =====================================================
        // MICROPHONES
        // =====================================================

        public string[] GetMicrophones()
        {
            using (var enumerator =
                   new MMDeviceEnumerator())
            {
                var devices =
                    enumerator.EnumerateAudioEndPoints(
                        DataFlow.Capture,
                        DeviceState.Active);

                var result =
                    new string[devices.Count];

                for (int i = 0;
                     i < devices.Count;
                     i++)
                {
                    result[i] =
                        devices[i].FriendlyName;
                }

                return result;
            }
        }

        // =====================================================
        // OUTPUT DEVICES
        // =====================================================

        public string[] GetOutputDevices()
        {
            using (var enumerator =
                   new MMDeviceEnumerator())
            {
                var devices =
                    enumerator.EnumerateAudioEndPoints(
                        DataFlow.Render,
                        DeviceState.Active);

                var result =
                    new string[devices.Count];

                for (int i = 0;
                     i < devices.Count;
                     i++)
                {
                    result[i] =
                        devices[i].FriendlyName;
                }

                return result;
            }
        }
    }
}