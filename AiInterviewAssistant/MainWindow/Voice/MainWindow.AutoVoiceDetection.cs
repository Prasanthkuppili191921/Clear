using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // AUTO VOICE DETECTION
        // =========================================================

        private WasapiLoopbackCapture autoVoiceSystemCapture;

        private WasapiCapture autoVoiceMicrophoneCapture;

        private bool autoVoiceDetectionRunning = false;

        private bool autoVoiceTriggered = false;

        private DateTime autoVoiceLastDetected =
            DateTime.MinValue;

        private readonly object autoVoiceDetectionLock =
            new object();


        // =========================================================
        // SETTINGS
        // =========================================================

        private bool ShouldAutoVoiceDetect()
        {
            return _chatGPTView &&
                   IsSmartAnswerEnabled;
        }


        // =========================================================
        // START AUTO VOICE DETECTION
        // =========================================================

        private void StartAutoVoiceDetection()
        {
            try
            {
                if (!ShouldAutoVoiceDetect())
                {
                    return;
                }

                if (autoVoiceDetectionRunning)
                {
                    return;
                }

                autoVoiceDetectionRunning = true;

                autoVoiceTriggered = false;

                autoVoiceLastDetected =
                    DateTime.MinValue;


                // =================================================
                // SYSTEM AUDIO
                // =================================================

                try
                {
                    autoVoiceSystemCapture =
                        new WasapiLoopbackCapture();

                    autoVoiceSystemCapture.DataAvailable +=
                        AutoVoiceSystem_DataAvailable;

                    autoVoiceSystemCapture.StartRecording();

                    Debug.WriteLine(
                        "AUTO VOICE: SYSTEM AUDIO DETECTION STARTED");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "AUTO VOICE: SYSTEM AUDIO ERROR = " +
                        ex.Message);

                    autoVoiceSystemCapture =
                        null;
                }


                // =================================================
                // MICROPHONE
                // =================================================

                try
                {
                    //var enumerator =
                    //    new MMDeviceEnumerator();

                    //var device =
                    //    enumerator.GetDefaultAudioEndpoint(
                    //        DataFlow.Capture,
                    //        Role.Communications);

                    //if (device != null)
                    //{
                    //    autoVoiceMicrophoneCapture =
                    //        new WasapiCapture(device);

                    //    autoVoiceMicrophoneCapture.DataAvailable +=
                    //        AutoVoiceMicrophone_DataAvailable;

                    //    autoVoiceMicrophoneCapture.StartRecording();

                    //    Debug.WriteLine(
                    //        "AUTO VOICE: MICROPHONE DETECTION STARTED");
                    //}
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "AUTO VOICE: MICROPHONE ERROR = " +
                        ex.Message);

                    autoVoiceMicrophoneCapture =
                        null;
                }


                Debug.WriteLine(
                    "AUTO VOICE DETECTION STARTED");
            }
            catch (Exception ex)
            {
                autoVoiceDetectionRunning = false;

                Debug.WriteLine(
                    "AUTO VOICE DETECTION START ERROR = " +
                    ex.Message);
            }
        }


        // =========================================================
        // SYSTEM AUDIO DATA
        // =========================================================

        private void AutoVoiceSystem_DataAvailable(
            object sender,
            WaveInEventArgs e)
        {
            try
            {
                if (!ShouldAutoVoiceDetect())
                {
                    return;
                }

                if (e == null ||
                    e.Buffer == null ||
                    e.BytesRecorded <= 0)
                {
                    return;
                }

                if (IsSpeechLikeAudio(
                    e.Buffer,
                    e.BytesRecorded))
                {
                    OnAutoVoiceDetected(
                        "SYSTEM");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "AUTO VOICE SYSTEM DETECTION ERROR = " +
                    ex.Message);
            }
        }


        // =========================================================
        // MICROPHONE DATA
        // =========================================================

        private void AutoVoiceMicrophone_DataAvailable(
            object sender,
            WaveInEventArgs e)
        {
            try
            {
                if (!ShouldAutoVoiceDetect())
                {
                    return;
                }

                if (e == null ||
                    e.Buffer == null ||
                    e.BytesRecorded <= 0)
                {
                    return;
                }

                if (IsSpeechLikeAudio(
                    e.Buffer,
                    e.BytesRecorded))
                {
                    OnAutoVoiceDetected(
                        "MICROPHONE");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "AUTO VOICE MICROPHONE DETECTION ERROR = " +
                    ex.Message);
            }
        }


        // =========================================================
        // AUDIO LEVEL DETECTION
        // =========================================================

        private bool IsSpeechLikeAudio(
            byte[] buffer,
            int bytesRecorded)
        {
            if (buffer == null ||
                bytesRecorded < 4)
            {
                return false;
            }

            int sampleCount = 0;

            double sumSquares = 0.0;

            double peak = 0.0;


            // =====================================================
            // WASAPI FLOAT32
            // =====================================================

            int step = 4;

            for (
                int i = 0;
                i + 3 < bytesRecorded;
                i += step)
            {
                float sample;

                try
                {
                    sample =
                        BitConverter.ToSingle(
                            buffer,
                            i);
                }
                catch
                {
                    continue;
                }


                if (float.IsNaN(sample) ||
                    float.IsInfinity(sample))
                {
                    continue;
                }


                double value =
                    Math.Abs(sample);


                if (value > 1.0)
                {
                    value = 1.0;
                }


                if (value > peak)
                {
                    peak = value;
                }


                sumSquares +=
                    value * value;

                sampleCount++;
            }


            if (sampleCount == 0)
            {
                return false;
            }


            double rms =
                Math.Sqrt(
                    sumSquares /
                    sampleCount);


            // =====================================================
            // DEBUG
            // =====================================================

            //Debug.WriteLine(
            //    "AUTO VOICE LEVEL | RMS=" +
            //    rms.ToString("0.0000") +
            //    " | PEAK=" +
            //    peak.ToString("0.0000"));


            // =====================================================
            // SPEECH THRESHOLD
            // =====================================================

            const double rmsThreshold =
                0.015;

            const double peakThreshold =
                0.035;


            return
                rms >= rmsThreshold ||
                peak >= peakThreshold;
        }


        // =========================================================
        // VOICE DETECTED
        // =========================================================

        private void OnAutoVoiceDetected(string source)
        {
            try
            {
                // ============================================
                // AUTO VOICE CONDITIONS
                // ============================================

                if (!_chatGPTView ||
                    !IsSmartAnswerEnabled)
                {
                    return;
                }

                // ============================================
                // ALREADY TRIGGERED
                // ============================================

                if (autoVoiceTriggered)
                {
                    return;
                }

                autoVoiceTriggered = true;

                Debug.WriteLine(
                    "AUTO VOICE DETECTED FROM: " +
                    source);

                // ============================================
                // START EXISTING VOICE FUNCTIONALITY
                // ============================================

                Dispatcher.BeginInvoke(
                    new Action(
                        async () =>
                        {
                            try
                            {
                                // Allow the detected speech/audio to settle
                                await Task.Delay(500);

                                StartVoiceRecording();

                                Debug.WriteLine(
                                    "AUTO VOICE: StartVoiceRecording() EXECUTED");
                            }
                            catch (Exception ex)
                            {
                                autoVoiceTriggered = false;

                                Debug.WriteLine(
                                    "AUTO VOICE: StartVoiceRecording ERROR = " +
                                    ex.Message);
                            }
                        }));
            }
            catch (Exception ex)
            {
                autoVoiceTriggered = false;

                Debug.WriteLine(
                    "AUTO VOICE DETECT ERROR = " +
                    ex.Message);
            }
        }


        // =========================================================
        // TURN CHATGPT VOICE ON
        // =========================================================

        private async Task TurnChatGPTVoiceOnAutomatically()
        {
            try
            {
                if (!ShouldAutoVoiceDetect())
                {
                    return;
                }


                Debug.WriteLine(
                    "AUTO VOICE: TURNING CHATGPT VOICE ON");


                // =================================================
                // HIDE TEXT INPUT
                // =================================================

                if (TextInputPanel != null)
                {
                    TextInputPanel.Visibility =
                        System.Windows.Visibility.Collapsed;
                }


                // =================================================
                // EXISTING CHATGPT VOICE PATH
                // =================================================

                await ChatGPTWebViewHost.StartVoiceIfNotActiveAsync();


                Debug.WriteLine(
                    "AUTO VOICE: CHATGPT VOICE TOGGLE SENT");
            }
            catch (Exception ex)
            {
                autoVoiceTriggered = false;

                Debug.WriteLine(
                    "AUTO VOICE ON ERROR = " +
                    ex.Message);
            }
        }


        // =========================================================
        // STOP AUTO VOICE DETECTION
        // =========================================================

        private void StopAutoVoiceDetection()
        {
            try
            {
                autoVoiceDetectionRunning = false;

                autoVoiceTriggered = false;

                autoVoiceLastDetected =
                    DateTime.MinValue;


                // =================================================
                // SYSTEM AUDIO
                // =================================================

                if (autoVoiceSystemCapture != null)
                {
                    try
                    {
                        autoVoiceSystemCapture.DataAvailable -=
                            AutoVoiceSystem_DataAvailable;

                        autoVoiceSystemCapture.StopRecording();
                    }
                    catch
                    {
                    }

                    try
                    {
                        autoVoiceSystemCapture.Dispose();
                    }
                    catch
                    {
                    }

                    autoVoiceSystemCapture =
                        null;
                }


                // =================================================
                // MICROPHONE
                // =================================================

                if (autoVoiceMicrophoneCapture != null)
                {
                    try
                    {
                        autoVoiceMicrophoneCapture.DataAvailable -=
                            AutoVoiceMicrophone_DataAvailable;

                        autoVoiceMicrophoneCapture.StopRecording();
                    }
                    catch
                    {
                    }

                    try
                    {
                        autoVoiceMicrophoneCapture.Dispose();
                    }
                    catch
                    {
                    }

                    autoVoiceMicrophoneCapture =
                        null;
                }


                Debug.WriteLine(
                    "AUTO VOICE DETECTION STOPPED");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "AUTO VOICE STOP ERROR = " +
                    ex.Message);
            }
        }
    }
}