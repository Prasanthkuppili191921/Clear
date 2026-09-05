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
            if (!IsVoiceInputEnabled())
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
                //
                // WASAPI LOOPBACK captures whatever is being
                // rendered by the selected/default Windows output.
                //
                // YouTube / Teams / Browser / Meet / Zoom etc.
                // can therefore enter the same pipeline.
                // =================================================

                voiceRecorder =
                    new WasapiLoopbackCapture();


                // =================================================
                // FORMAT
                // =================================================

                voiceRecordingFormat =
                    voiceRecorder.WaveFormat;

                Debug.WriteLine(
                    "========================================");

                Debug.WriteLine(
                    "VOICE CAPTURE START");

                Debug.WriteLine(
                    "VOICE FORMAT = " +
                    voiceRecordingFormat.Encoding +
                    " | " +
                    voiceRecordingFormat.SampleRate +
                    "Hz | " +
                    voiceRecordingFormat.BitsPerSample +
                    "bit | " +
                    voiceRecordingFormat.Channels +
                    "ch");


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


                // =================================================
                // START LOCAL MICROPHONE
                // =================================================

                localVoiceStopped = true;

                if (IsLocalVoiceEnabled())
                {
                    try
                    {
                        localVoiceRecorder =
                            new LocalVoiceCapture();

                        localVoiceRecorder.RecordingStopped +=
                            localVoiceRecorder_RecordingStopped;

                        localVoiceStopped = false;

                        localVoiceRecorder.Start();

                        localVoiceRecordingFormat =
                            localVoiceRecorder.WaveFormat;

                        Debug.WriteLine(
                            "LOCAL MICROPHONE CAPTURE STARTED");

                        Debug.WriteLine(
                            "LOCAL VOICE FORMAT = " +
                            localVoiceRecordingFormat.Encoding +
                            " | " +
                            localVoiceRecordingFormat.SampleRate +
                            "Hz | " +
                            localVoiceRecordingFormat.BitsPerSample +
                            "bit | " +
                            localVoiceRecordingFormat.Channels +
                            "ch");
                       
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "LOCAL MICROPHONE START ERROR: " +
                            ex);

                        try
                        {
                            localVoiceRecorder?.Dispose();
                        }
                        catch
                        {
                        }

                        localVoiceRecorder = null;
                        localVoiceRecordingFormat = null;
                        localVoiceStopped = true;
                    }
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

                    VoiceButton.ToolTip =
                        "Stop Voice Input";
                }


                // =================================================
                // PULSE
                // =================================================

                if (VoicePulseScale != null &&
                    voicePulseAnimation != null)
                {
                    VoicePulseScale.BeginAnimation(
                        System.Windows.Media.ScaleTransform.ScaleXProperty,
                        voicePulseAnimation);

                    VoicePulseScale.BeginAnimation(
                        System.Windows.Media.ScaleTransform.ScaleYProperty,
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

            if (voiceRecorder == null)
            {
                return;
            }

            if (voiceStopping)
            {
                return;
            }

            voiceStopping = true;

            // =========================================================
            // RECORDING IS NOW OFF
            // =========================================================

            isVoiceRecording = false;

            // Immediately restore microphone button/icon
            ResetVoiceUI();

            // Keep processing message if desired
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

                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateLiveVoiceMessage(
                        "Processing...");
                });

                //// =================================================
                //// COPY AUDIO
                //// =================================================

                //byte[] rawAudio;

                //lock (voiceAudioLock)
                //{
                //    if (voiceAudioBuffer == null ||
                //        voiceAudioBuffer.Length == 0)
                //    {
                //        rawAudio = null;
                //    }
                //    else
                //    {
                //        rawAudio =
                //            voiceAudioBuffer.ToArray();
                //    }
                //}

                // =================================================
                // WAIT FOR LOCAL MICROPHONE TO STOP
                // =================================================

                if (IsLocalVoiceEnabled() &&
                    localVoiceRecorder != null)
                {
                    int waitCount = 0;

                    while (!localVoiceStopped &&
                           waitCount < 100)
                    {
                        await Task.Delay(20);

                        waitCount++;
                    }
                }


                // =================================================
                // COPY SYSTEM AUDIO
                // =================================================

                byte[] systemRawAudio;

                lock (voiceAudioLock)
                {
                    if (voiceAudioBuffer == null ||
                        voiceAudioBuffer.Length == 0)
                    {
                        systemRawAudio = null;
                    }
                    else
                    {
                        systemRawAudio =
                            voiceAudioBuffer.ToArray();
                    }
                }


                // =================================================
                // COPY LOCAL MICROPHONE AUDIO
                // =================================================

                byte[] localRawAudio = null;

                if (IsLocalVoiceEnabled() &&
                    localVoiceRecorder != null)
                {
                    try
                    {
                        localRawAudio =
                            localVoiceRecorder.GetAudioBytes();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "LOCAL AUDIO READ ERROR: " +
                            ex);
                    }
                }


                // =================================================
                // COMBINE AUDIO
                // =================================================

                byte[] rawAudio;

                if (IsLocalVoiceEnabled() &&
                    localRawAudio != null &&
                    localRawAudio.Length > 0)
                {
                    rawAudio =
                        CombineVoiceAudio(
                            systemRawAudio,
                            voiceRecordingFormat,
                            localRawAudio,
                            localVoiceRecordingFormat);
                }
                else
                {
                    // IncludeLocalVoice = false
                    // Existing system-audio path remains unchanged.
                    rawAudio = systemRawAudio;
                }

                // =================================================
                // NO AUDIO
                // =================================================

                if (rawAudio == null ||
                    rawAudio.Length == 0)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        RemoveLiveVoiceMessage();

                        AppMessage.Show(
                            "No voice input was captured.");
                    });

                    return;
                }

                Debug.WriteLine(
                    "VOICE CAPTURED BYTES = " +
                    rawAudio.Length);

                // =================================================
                // CREATE WAV
                //
                // Keep this because Deepgram accepts WAV.
                // WAV is created in memory only.
                // Nothing is saved to disk.
                // =================================================

                byte[] wavBytes;

                if (IsLocalVoiceEnabled() &&
                    localRawAudio != null &&
                    localRawAudio.Length > 0)
                {
                    // CombineVoiceAudio returns FINAL WAV
                    wavBytes = rawAudio;
                }
                else
                {
                    // Existing system-audio path
                    wavBytes = CreateWavBytes(rawAudio);
                }

                rawAudio = null;

                if (wavBytes == null ||
                    wavBytes.Length <= 44)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        RemoveLiveVoiceMessage();

                        AppMessage.Show(
                            "Could not prepare the captured voice audio.");
                    });

                    return;
                }

                Debug.WriteLine(
                    "WAV MEMORY BYTES = " +
                    wavBytes.Length);

                // =================================================
                // DEEPGRAM DIRECT STT
                // =================================================

                Stopwatch timer =
                    Stopwatch.StartNew();

                Debug.WriteLine(
                    "========================================");

                Debug.WriteLine(
                    "DEEPGRAM DIRECT STT START");

                string finalText =
                    await TranscribeWithOpenRouterAsync(
                        wavBytes);

                timer.Stop();

                Debug.WriteLine(
                    "DEEPGRAM STT TIME = " +
                    timer.ElapsedMilliseconds +
                    " ms");

                Debug.WriteLine(
                    "DEEPGRAM FINAL TEXT = [" +
                    finalText +
                    "]");

                Debug.WriteLine(
                    "========================================");

                // =================================================
                // NO TEXT
                // =================================================

                if (string.IsNullOrWhiteSpace(
                    finalText))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        RemoveLiveVoiceMessage();

                        AppMessage.Show(
                            "No speech was detected in the voice input.");
                    });

                    return;
                }

                // =================================================
                // CLEAN TEXT
                // =================================================

                finalText =
                    finalText.Trim();

                liveVoiceTranscript =
                    finalText;

                // =================================================
                // CHAT UI
                // =================================================

                Border thinkingBubble =
                    null;

                await Dispatcher.InvokeAsync(() =>
                {
                    RemoveLiveVoiceMessage();

                    AddUserMessage(
                        finalText);

                    thinkingBubble =
                        AddAIMessage("");
                });

                // =================================================
                // SEND TO AI
                // =================================================

                Debug.WriteLine(
                 "SENDING COMPLETE VOICE TEXT TO AI...");

                _ = SendQuestion(
                    finalText,
                    thinkingBubble);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE PROCESSING ERROR:");

                Debug.WriteLine(
                    ex.ToString());

                await Dispatcher.InvokeAsync(() =>
                {
                    RemoveLiveVoiceMessage();

                    AppMessage.Show(
                        "Voice processing error:\n\n" +
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