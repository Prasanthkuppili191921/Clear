using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MessageBox = System.Windows.Forms.MessageBox;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // AUDIO DIAGNOSTIC
        // =========================================================

        private long voiceTotalBytes;

        private long voiceNonZeroBytes;

        private DateTime voiceLastDiagnostic =
            DateTime.MinValue;

        // =========================================================
        // TOGGLE VOICE
        // =========================================================
        public void ToggleVoiceRecording()
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            ToggleVoiceRecording();
                        }));

                    return;
                }

                if (isVoiceRecording &&
                    voiceRecorder != null)
                {
                    StopVoiceRecording();
                    return;
                }

                if (voiceStopping)
                {
                    return;
                }

                // =====================================================
                // NEW VOICE CYCLE MUST BE PREPARED BEFORE STARTING
                // THROUGH THIS VOICE TOGGLE PATH AS WELL.
                // =====================================================

                if (!_chatGPTView)
                {
                    PrepareVoiceCycleForNewRecording();
                }

                StartVoiceRecording();
            }
            catch (Exception ex)
            {
                AppMessage.Show(
                    "Voice toggle error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // START RECORDING
        // =========================================================

        private void StartVoiceRecording()
        {
            if (!_chatGPTView && !IsVoiceInputEnabled())
            {
                AppMessage.Show(
                    "Voice Input is currently disabled.\n\n" +
                    "Please enable Voice Input from Settings → Voice.",
                    "Voice Input",
                    MessageBoxButton.OK);

                return;
            }

            if (voiceRecorder != null ||
                isVoiceRecording ||
                voiceStopping)
            {
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        StartVoiceRecording();
                    }));

                return;
            }

            try
            {
                // =================================================
                // VOICE MODE
                // =================================================

                voiceStopping = false;
                isVoiceRecording = false;

                liveVoiceTranscript =
                    string.Empty;

                voiceRecordingFormat =
                    null;

                voiceTotalBytes = 0;
                voiceNonZeroBytes = 0;
                voiceLastDiagnostic =
                    DateTime.Now;


                // =================================================
                // STT CHECK
                // =================================================

                if (!InitializeSpeechToText())
                {
                    return;
                }


                // =================================================
                // NEW AUDIO BUFFER
                // =================================================

                lock (voiceAudioLock)
                {
                    voiceAudioBuffer =
                        new System.IO.MemoryStream();
                }


                // =================================================
                // WINDOWS SYSTEM AUDIO
                // =================================================

                voiceRecorder =
                    new WasapiLoopbackCapture();


                // =================================================
                // FORMAT
                // =================================================

                voiceRecordingFormat =
                    voiceRecorder.WaveFormat;

                Debug.WriteLine(
                    "SYSTEM AUDIO FORMAT: " +
                    voiceRecordingFormat);


                // =================================================
                // AUDIO DATA
                // =================================================

                voiceRecorder.DataAvailable +=
                    VoiceRecorder_DataAvailable;


                // =================================================
                // STOP EVENT
                // =================================================

                voiceRecorder.RecordingStopped +=
                    VoiceRecorder_RecordingStopped;


                // =================================================
                // START SYSTEM AUDIO
                // =================================================

                voiceRecorder.StartRecording();


                //// =================================================
                //// START LOCAL MICROPHONE
                //// =================================================

                //localVoiceStopped = true;

                //if (IsLocalVoiceEnabled())
                //{
                //    try
                //    {
                //        localVoiceRecorder =
                //            new LocalVoiceCapture();

                //        localVoiceRecorder.RecordingStopped +=
                //            localVoiceRecorder_RecordingStopped;

                //        localVoiceStopped = false;

                //        localVoiceRecorder.Start();

                //        localVoiceRecordingFormat =
                //            localVoiceRecorder.WaveFormat;

                //        Debug.WriteLine(
                //            "LOCAL MICROPHONE CAPTURE STARTED");

                //        Debug.WriteLine(
                //            "LOCAL MICROPHONE FORMAT: " +
                //            localVoiceRecordingFormat);
                //    }
                //    catch (Exception ex)
                //    {
                //        Debug.WriteLine(
                //            "LOCAL MICROPHONE START ERROR: " +
                //            ex.Message);

                //        localVoiceStopped = true;
                //    }
                //}

                // =========================================================
                // START CONTINUOUS SILERO VAD
                // =========================================================

                if (!_chatGPTView)
                {
                    try
                    {
                        lock (voiceSessionLock)
                        {
                            voiceSessionSpeechSegments.Clear();
                        }

                        voiceVadSession =
                            new SileroVadSession();

                        voiceVadSession.SpeechSegmentReady +=
                            VoiceVadSession_SpeechSegmentReady;

                        voiceVadSession.Start(
                            voiceRecordingFormat);

                        System.Diagnostics.Debug.WriteLine(
                            "SILERO VAD SESSION STARTED");
                    }
                    catch (Exception ex)
                    {
                        voiceVadSession = null;

                        System.Diagnostics.Debug.WriteLine(
                            "SILERO VAD SESSION START ERROR: " +
                            ex);
                    }
                }

                // =========================================================
                // START QUESTION QUEUE
                // =========================================================

                if (!_chatGPTView)
                {
                    StartVoiceQuestionQueue();
                }

                isVoiceRecording = true;


                // =================================================
                // UI
                // =================================================

                SetVoiceInputMode(true);

                CreateLiveVoiceMessage();

                UpdateLiveVoiceMessage(
                    "Listening...");


                // =================================================
                // BUTTON
                // =================================================

                if (VoiceButton != null)
                {
                    VoiceButton.Background =
                        new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(
                                190,
                                45,
                                45));
                }


                // =================================================
                // PULSE
                // =================================================

                if (VoicePulseScale != null &&
                    voicePulseAnimation != null)
                {
                    VoicePulseScale.BeginAnimation(
                        ScaleTransform.ScaleXProperty,
                        voicePulseAnimation);

                    VoicePulseScale.BeginAnimation(
                        ScaleTransform.ScaleYProperty,
                        voicePulseAnimation);
                }


                Debug.WriteLine(
                    "VOICE RECORDING STARTED");

                Debug.WriteLine(
                    "========================================");
            }
            catch (Exception ex)
            {
                CleanupVoiceRecorder();

                RemoveLiveVoiceMessage();

                ResetVoiceUI();

                AppMessage.Show(
                    "System audio capture error:\n\n" +
                    ex.Message);
            }
        }


        // =========================================================
        // AUDIO DATA
        // =========================================================

        private void VoiceRecorder_DataAvailable(
            object sender,
            WaveInEventArgs e)
        {
            try
            {
                if (e == null ||
                    e.BytesRecorded <= 0)
                {
                    return;
                }

                voiceTotalBytes +=
                    e.BytesRecorded;


                // =================================================
                // CHECK WHETHER AUDIO IS ACTUALLY NON-SILENT
                // =================================================

                int nonZero =
                    0;

                int step =
                    Math.Max(
                        1,
                        e.BytesRecorded / 4096);

                for (
                    int i = 0;
                    i < e.BytesRecorded;
                    i += step)
                {
                    if (e.Buffer[i] != 0)
                    {
                        nonZero++;
                    }
                }

                voiceNonZeroBytes +=
                    nonZero;


                // =================================================
                // BUFFER
                // =================================================

                lock (voiceAudioLock)
                {
                    if (voiceAudioBuffer != null)
                    {
                        voiceAudioBuffer.Write(
                            e.Buffer,
                            0,
                            e.BytesRecorded);
                    }
                }

                // =========================================================
                // CONTINUOUS SILERO VAD
                // =========================================================

                if (!_chatGPTView &&
                    isVoiceRecording &&
                    e.Buffer != null &&
                    e.BytesRecorded > 0)
                {
                    try
                    {
                        lock (voiceVadLock)
                        {
                            if (voiceVadSession != null)
                            {
                                voiceVadSession.AcceptAudio(
                                    e.Buffer,
                                    e.BytesRecorded);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "SILERO VAD AUDIO ERROR: " +
                            ex);
                    }
                }


                // =================================================
                // DIAGNOSTIC EVERY ~1 SECOND
                // =================================================

                if (
                    (DateTime.Now -
                     voiceLastDiagnostic)
                    .TotalSeconds >= 1)
                {
                    voiceLastDiagnostic =
                        DateTime.Now;

                    Debug.WriteLine(
                        "VOICE AUDIO: " +
                        "bytes=" +
                        voiceTotalBytes +
                        " | nonZero=" +
                        voiceNonZeroBytes);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Voice audio buffer error: " +
                    ex.Message);
            }
        }


        // =========================================================
        // STOP RECORDING
        // =========================================================

        // =========================================================
        // STOP RECORDING
        // =========================================================

        private void StopVoiceRecording()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        StopVoiceRecording();
                    }));

                return;
            }

            // =========================================================
            // CHECK SYSTEM AUDIO RECORDER
            // =========================================================

            if (voiceRecorder == null)
            {
                return;
            }

            if (voiceStopping)
            {
                return;
            }

            // =========================================================
            // MARK STOPPING
            // =========================================================

            voiceStopping = true;

            // =========================================================
            // IMPORTANT
            //
            // Keep isVoiceRecording = true until
            // WasapiLoopbackCapture raises RecordingStopped.
            //
            // This allows the final DataAvailable event to reach
            // Silero VAD before the VAD session is flushed.
            // =========================================================

            isVoiceRecording = true;

            // =========================================================
            // IMMEDIATELY RESTORE VOICE BUTTON UI
            // =========================================================

            ResetVoiceUI();

            // =========================================================
            // SHOW PROCESSING
            // =========================================================

            UpdateLiveVoiceMessage(
                "Processing...");

            try
            {
                // =====================================================
                // STOP LOCAL MICROPHONE
                // =====================================================

                if (localVoiceRecorder != null &&
                    localVoiceRecorder.IsRecording)
                {
                    try
                    {
                        localVoiceRecorder.Stop();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "Stop local microphone error: " +
                            ex.Message);

                        localVoiceStopped = true;
                    }
                }
                else
                {
                    localVoiceStopped = true;
                }

                // =====================================================
                // IMPORTANT:
                //
                // DO NOT STOP SILERO VAD HERE.
                //
                // WasapiLoopbackCapture may still send its final
                // DataAvailable event.
                //
                // VAD will be stopped/flushed inside
                // VoiceRecorder_RecordingStopped().
                // =====================================================

                // =====================================================
                // STOP SYSTEM AUDIO
                // =====================================================

                voiceRecorder.StopRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Stop voice error: " +
                    ex.Message);

                voiceStopping = false;
                isVoiceRecording = false;

                ResetVoiceUI();

                RemoveLiveVoiceMessage();
            }
        }

        // =========================================================
        // SILERO VAD SPEECH SEGMENT READY
        // =========================================================

        private void VoiceVadSession_SpeechSegmentReady(
            object sender,
            byte[] speechWav)
        {
            try
            {
                if (_chatGPTView)
                    return;

                if (speechWav == null ||
                    speechWav.Length <= 44)
                {
                    return;
                }

                Debug.WriteLine(
                    "SILERO VAD: SPEECH SEGMENT RECEIVED | SIZE = " +
                    speechWav.Length);

                lock (voiceSessionLock)
                {
                    voiceSessionSpeechSegments.Add(
                        speechWav);
                }

                Debug.WriteLine(
                    "VOICE SESSION: SPEECH SEGMENT ADDED | SEGMENTS = " +
                    voiceSessionSpeechSegments.Count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE SESSION SEGMENT ERROR: " +
                    ex);
            }
        }



        // =========================================================
        // RECORDING STOPPED
        // =========================================================

        private async void VoiceRecorder_RecordingStopped(
    object sender,
    StoppedEventArgs e)
        {
            WasapiLoopbackCapture recorder =
                voiceRecorder;

            // =====================================================
            // PRESERVE FORMAT
            // =====================================================

            if (recorder?.WaveFormat != null)
            {
                voiceRecordingFormat =
                    recorder.WaveFormat;
            }

            // =====================================================
            // STATE
            // =====================================================

            voiceRecorder = null;
            isVoiceRecording = false;

            try
            {
                // =================================================
                // RECORDING ERROR
                // =================================================

                if (e.Exception != null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        RemoveLiveVoiceMessage();

                        AppMessage.Show(
                            "System audio recording error:\n\n" +
                            e.Exception.Message);
                    });

                    return;
                }

                // =================================================
                // DIAGNOSTIC
                // =================================================

                Debug.WriteLine(
                    "========================================");

                Debug.WriteLine(
                    "VOICE RECORDING STOPPED");

                Debug.WriteLine(
                    "TOTAL CAPTURED BYTES = " +
                    voiceTotalBytes);

                Debug.WriteLine(
                    "NON-ZERO AUDIO BYTES = " +
                    voiceNonZeroBytes);

                Debug.WriteLine(
                    "========================================");

                // =================================================
                // LOCAL VOICE
                // =================================================

                if (!_chatGPTView)
                {
                    // =====================================================
                    // FINAL VAD FLUSH
                    //
                    // IMPORTANT:
                    //
                    // SileroVadSession.Stop() performs the final
                    // DrainSpeechSegments().
                    //
                    // SpeechSegmentReady is raised synchronously
                    // during Stop().
                    //
                    // Therefore:
                    //
                    // 1. Stop VAD first
                    // 2. Let final SpeechSegmentReady callback execute
                    // 3. Unsubscribe
                    // 4. Dispose VAD
                    // 5. Build session WAV
                    //
                    // DO NOT unsubscribe before Stop().
                    // =====================================================

                    try
                    {
                        lock (voiceVadLock)
                        {
                            if (voiceVadSession != null)
                            {
                                voiceVadSession.Stop();

                                voiceVadSession.SpeechSegmentReady -=
                                    VoiceVadSession_SpeechSegmentReady;

                                voiceVadSession.Dispose();

                                voiceVadSession = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "SILERO VAD STOP ERROR: " +
                            ex);
                    }

                    Debug.WriteLine(
                        "VOICE STOP: NEW QUESTIONS DISABLED");

                    Debug.WriteLine(
                        "VOICE STOP: FINALIZING CURRENT VOICE SESSION");

                    // =====================================================
                    // BUILD SESSION FROM VAD SPEECH SEGMENTS
                    // =====================================================

                    byte[] sessionWav =
                        BuildVoiceSessionWav();

                    if (sessionWav != null &&
                        sessionWav.Length > 44)
                    {
                        Debug.WriteLine(
                            "VOICE SESSION WAV READY | SIZE = " +
                            sessionWav.Length);

                        EnqueueVoiceSession(
                            sessionWav);
                    }
                    else
                    {
                        Debug.WriteLine(
                            "VOICE SESSION: NO SPEECH DETECTED");
                    }

                    Debug.WriteLine(
                        "VOICE STOP: EXISTING QUEUE PROCESSING CONTINUES");

                    RemoveLiveVoiceMessage();

                    return;
                }

                // =================================================
                // CHATGPT WEBVIEW
                //
                // Keep existing ChatGPT behavior untouched.
                // =================================================

                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateLiveVoiceMessage(
                        "Processing...");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE RECORDING STOP ERROR:");

                Debug.WriteLine(
                    ex.ToString());

                await Dispatcher.InvokeAsync(() =>
                {
                    RemoveLiveVoiceMessage();

                    AppMessage.Show(
                        "Voice recording error:\n\n" +
                        ex.Message);
                });
            }
            finally
            {
                // =================================================
                // DISPOSE SYSTEM RECORDER
                // =================================================

                try
                {
                    recorder?.Dispose();
                }
                catch
                {
                }

                // =================================================
                // DISPOSE LOCAL MICROPHONE
                // =================================================

                try
                {
                    if (localVoiceRecorder != null)
                    {
                        localVoiceRecorder.RecordingStopped -=
                            localVoiceRecorder_RecordingStopped;

                        localVoiceRecorder.Dispose();
                    }
                }
                catch
                {
                }

                localVoiceRecorder = null;
                localVoiceRecordingFormat = null;
                localVoiceStopped = true;

                // =================================================
                // CLEAR AUDIO BUFFER
                //
                // No STT/VAD processing happens here.
                // =================================================

                lock (voiceAudioLock)
                {
                    try
                    {
                        voiceAudioBuffer?.Dispose();
                    }
                    catch
                    {
                    }

                    voiceAudioBuffer = null;
                }

                // =================================================
                // RESET STATE
                // =================================================

                voiceRecordingFormat = null;

                isVoiceRecording = false;

                voiceStopping = false;

                // =================================================
                // RESET UI
                // =================================================

                await Dispatcher.InvokeAsync(() =>
                {
                    ResetVoiceUI();
                });
            }
        }


        // =========================================================
        // COMBINE SYSTEM AUDIO + LOCAL MICROPHONE AUDIO
        // =========================================================

        private byte[] CombineVoiceAudio(
            byte[] systemRawAudio,
            WaveFormat systemFormat,
            byte[] localRawAudio,
            WaveFormat localFormat)
        {
            try
            {
                // =====================================================
                // SYSTEM AUDIO NOT AVAILABLE
                // =====================================================

                if (systemRawAudio == null ||
                    systemRawAudio.Length == 0)
                {
                    return CreateWavBytesFromFormat(
                        localRawAudio,
                        localFormat);
                }


                // =====================================================
                // LOCAL MICROPHONE NOT AVAILABLE
                // =====================================================

                if (localRawAudio == null ||
                    localRawAudio.Length == 0)
                {
                    return CreateWavBytesFromFormat(
                        systemRawAudio,
                        systemFormat);
                }


                // =====================================================
                // INVALID FORMAT
                // =====================================================

                if (systemFormat == null ||
                    localFormat == null)
                {
                    return null;
                }


                // =====================================================
                // CONVERT BOTH SOURCES
                // TO 16K MONO PCM16 WAV
                // =====================================================

                byte[] systemWav =
                    CreateWavBytesFromFormat(
                        systemRawAudio,
                        systemFormat);

                byte[] localWav =
                    CreateWavBytesFromFormat(
                        localRawAudio,
                        localFormat);


                // =====================================================
                // SYSTEM CONVERSION FAILED
                // =====================================================

                if (systemWav == null ||
                    systemWav.Length <= 44)
                {
                    // Return already converted local WAV
                    return localWav;
                }


                // =====================================================
                // LOCAL CONVERSION FAILED
                // =====================================================

                if (localWav == null ||
                    localWav.Length <= 44)
                {
                    // Return already converted system WAV
                    return systemWav;
                }


                // =====================================================
                // EXTRACT PCM DATA
                // =====================================================

                int systemDataLength =
                    systemWav.Length - 44;

                int localDataLength =
                    localWav.Length - 44;

                int outputLength =
                    Math.Max(
                        systemDataLength,
                        localDataLength);

                byte[] output =
                    new byte[outputLength];


                // =====================================================
                // MIX PCM16 SAMPLES
                // =====================================================

                for (int i = 0;
                     i + 1 < outputLength;
                     i += 2)
                {
                    short systemSample = 0;
                    short localSample = 0;


                    if (i + 1 < systemDataLength)
                    {
                        systemSample =
                            BitConverter.ToInt16(
                                systemWav,
                                44 + i);
                    }


                    if (i + 1 < localDataLength)
                    {
                        localSample =
                            BitConverter.ToInt16(
                                localWav,
                                44 + i);
                    }


                    // Average both sources
                    int mixed =
                        (systemSample + localSample) / 2;


                    // Safe clamp
                    if (mixed > short.MaxValue)
                    {
                        mixed = short.MaxValue;
                    }
                    else if (mixed < short.MinValue)
                    {
                        mixed = short.MinValue;
                    }


                    byte[] bytes =
                        BitConverter.GetBytes(
                            (short)mixed);


                    output[i] =
                        bytes[0];

                    output[i + 1] =
                        bytes[1];
                }


                // =====================================================
                // CREATE FINAL 16K MONO PCM16 WAV
                // =====================================================

                return CreatePcm16Wav(
                    output,
                    16000,
                    1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "COMBINE AUDIO ERROR: " +
                    ex);

                // IMPORTANT:
                // Do not return raw audio here.
                // Caller expects a final WAV when
                // IncludeLocalVoice = true.

                return null;
            }
        }


        // =========================================================
        // CONVERT RAW AUDIO TO STANDARD WAV
        // =========================================================

        private byte[] CreateWavBytesFromFormat(
            byte[] rawAudio,
            WaveFormat sourceFormat)
        {
            if (rawAudio == null ||
                rawAudio.Length == 0 ||
                sourceFormat == null)
            {
                return null;
            }

            WaveFormat previousFormat =
                voiceRecordingFormat;

            try
            {
                voiceRecordingFormat =
                    sourceFormat;

                return CreateWavBytes(rawAudio);
            }
            finally
            {
                voiceRecordingFormat =
                    previousFormat;
            }
        }


        // =========================================================
        // CREATE PCM16 WAV
        // =========================================================

        private byte[] CreatePcm16Wav(
            byte[] pcmData,
            int sampleRate,
            int channels)
        {
            if (pcmData == null)
                return null;

            using (MemoryStream ms =
                new MemoryStream())
            {
                using (BinaryWriter writer =
                    new BinaryWriter(ms))
                {
                    int byteRate =
                        sampleRate *
                        channels *
                        2;

                    short blockAlign =
                        (short)(channels * 2);

                    writer.Write(
                        new[] { 'R', 'I', 'F', 'F' });

                    writer.Write(
                        36 + pcmData.Length);

                    writer.Write(
                        new[] { 'W', 'A', 'V', 'E' });

                    writer.Write(
                        new[] { 'f', 'm', 't', ' ' });

                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)channels);
                    writer.Write(sampleRate);
                    writer.Write(byteRate);
                    writer.Write(blockAlign);
                    writer.Write((short)16);

                    writer.Write(
                        new[] { 'd', 'a', 't', 'a' });

                    writer.Write(
                        pcmData.Length);

                    writer.Write(pcmData);

                    writer.Flush();

                    return ms.ToArray();
                }
            }
        }


        // =========================================================
        // LOCAL MICROPHONE STOPPED
        // =========================================================

        private void localVoiceRecorder_RecordingStopped(
            object sender,
            StoppedEventArgs e)
        {
            localVoiceStopped = true;

            Debug.WriteLine(
                "LOCAL MICROPHONE CAPTURE STOPPED");

            if (e.Exception != null)
            {
                Debug.WriteLine(
                    "LOCAL MICROPHONE ERROR: " +
                    e.Exception);
            }
        }


        // =========================================================
        // CLEANUP
        // =========================================================

        private void CleanupVoiceRecorder()
        {
            try
            {
                voiceRecorder?.Dispose();
            }
            catch
            {
            }

            voiceRecorder = null;


            // =====================================================
            // CLEANUP LOCAL MICROPHONE
            // =====================================================

            try
            {
                if (localVoiceRecorder != null)
                {
                    localVoiceRecorder.RecordingStopped -=
                        localVoiceRecorder_RecordingStopped;

                    localVoiceRecorder.Dispose();
                }
            }
            catch
            {
            }

            localVoiceRecorder = null;
            localVoiceRecordingFormat = null;
            localVoiceStopped = true;


            lock (voiceAudioLock)
            {
                try
                {
                    voiceAudioBuffer?.Dispose();
                }
                catch
                {
                }

                voiceAudioBuffer = null;
            }

            voiceRecordingFormat = null;

            isVoiceRecording = false;
            voiceStopping = false;
        }
    }
}