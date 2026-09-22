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
using System.Collections.Generic;

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
        // CONTINUOUS AUTO VOICE PRE-BUFFER
        // =========================================================

        private readonly object autoVoicePreBufferLock =
            new object();

        private byte[] autoVoicePreBuffer;

        private int autoVoicePreBufferWritePosition;

        private int autoVoicePreBufferCount;

        private const double AutoVoicePreBufferSeconds =
            2.0;

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


                // =====================================================
                // VOICE OFF
                // =====================================================

                if (isVoiceRecording)
                {
                    StopVoiceRecording();
                    return;
                }


                if (voiceStopping)
                {
                    return;
                }


                // =====================================================
                // NEW VOICE CYCLE
                // =====================================================

                PrepareVoiceCycleForNewRecording();


                // =====================================================
                // IMPORTANT:
                //
                // If Auto Voice is already ON,
                // WasapiLoopbackCapture is already running.
                //
                // StartVoiceRecording() will NOT create
                // another capture.
                // =====================================================

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
                // =====================================================
                // CAPTURE MUST EXIST
                //
                // Auto Voice ON:
                //     capture already exists.
                //
                // Manual Voice:
                //     create capture here.
                // =====================================================

                if (voiceRecorder == null)
                {
                    StartContinuousVoiceCapture();
                }


                if (voiceRecorder == null)
                {
                    Debug.WriteLine(
                        "VOICE: CAPTURE NOT AVAILABLE");

                    return;
                }


                // =====================================================
                // ALREADY IN A VOICE CYCLE
                // =====================================================

                if (isVoiceRecording)
                {
                    return;
                }


                if (voiceStopping)
                {
                    return;
                }


                // =====================================================
                // START NEW VOICE CYCLE
                // =====================================================

                StartVoiceCycleFromCurrentCapture();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE START ERROR: " +
                    ex);


                CleanupVoiceRecorder();

                RemoveLiveVoiceMessage();

                ResetVoiceUI();


                AppMessage.Show(
                    "System audio capture error:\n\n" +
                    ex.Message);
            }
        }

        // =========================================================
        // START CONTINUOUS SYSTEM AUDIO CAPTURE
        // =========================================================

        private void StartContinuousVoiceCapture()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        StartContinuousVoiceCapture();
                    }));

                return;
            }


            if (voiceRecorder != null)
            {
                return;
            }


            try
            {
                voiceStopping = false;


                // =====================================================
                // CREATE WASAPI LOOPBACK CAPTURE
                // =====================================================

                voiceRecorder =
                    new WasapiLoopbackCapture();


                voiceRecordingFormat =
                    voiceRecorder.WaveFormat;


                Debug.WriteLine(
                    "SYSTEM AUDIO FORMAT: " +
                    voiceRecordingFormat);


                // =====================================================
                // INITIALIZE ROLLING PRE-BUFFER
                // =====================================================

                InitializeAutoVoicePreBuffer(
                    voiceRecordingFormat);


                // =====================================================
                // EVENTS
                // =====================================================

                voiceRecorder.DataAvailable +=
                    VoiceRecorder_DataAvailable;


                voiceRecorder.RecordingStopped +=
                    VoiceRecorder_RecordingStopped;


                // =====================================================
                // START CAPTURE
                // =====================================================

                voiceRecorder.StartRecording();


                Debug.WriteLine(
                    "CONTINUOUS SYSTEM AUDIO CAPTURE STARTED");
            }
            catch
            {
                try
                {
                    if (voiceRecorder != null)
                    {
                        voiceRecorder.DataAvailable -=
                            VoiceRecorder_DataAvailable;

                        voiceRecorder.RecordingStopped -=
                            VoiceRecorder_RecordingStopped;

                        voiceRecorder.Dispose();
                    }
                }
                catch
                {
                }


                voiceRecorder = null;

                voiceRecordingFormat = null;

                throw;
            }
        }

        // =========================================================
        // START VOICE CYCLE
        //
        // IMPORTANT:
        // This DOES NOT start WasapiLoopbackCapture.
        //
        // Capture is already running continuously.
        // =========================================================

        private void StartVoiceCycleFromCurrentCapture()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        StartVoiceCycleFromCurrentCapture();
                    }));

                return;
            }


            if (voiceRecorder == null)
            {
                return;
            }


            if (isVoiceRecording)
            {
                return;
            }


            if (voiceStopping)
            {
                return;
            }


            try
            {
                voiceStopping = false;

                isVoiceRecording = false;


                liveVoiceTranscript =
                    string.Empty;


                voiceTotalBytes = 0;

                voiceNonZeroBytes = 0;

                voiceLastDiagnostic =
                    DateTime.Now;


                // =====================================================
                // STT CHECK
                // =====================================================

                if (!InitializeSpeechToText())
                {
                    return;
                }


                // =====================================================
                // NEW CYCLE BUFFER
                // =====================================================

                lock (voiceAudioLock)
                {
                    try
                    {
                        voiceAudioBuffer?.Dispose();
                    }
                    catch
                    {
                    }


                    voiceAudioBuffer =
                        new MemoryStream();


                    // =================================================
                    // IMPORTANT:
                    //
                    // Copy the last 2 seconds BEFORE detection.
                    //
                    // This protects the first words of the question.
                    // =================================================

                    byte[] preBuffer =
                        GetAutoVoicePreBufferSnapshot();


                    if (preBuffer != null &&
                        preBuffer.Length > 0)
                    {
                        voiceAudioBuffer.Write(
                            preBuffer,
                            0,
                            preBuffer.Length);
                    }
                }


                // =====================================================
                // START NEW VAD SESSION
                // =====================================================

                try
                {
                    lock (voiceSessionLock)
                    {
                        voiceSessionSpeechSegments.Clear();
                    }


                    lock (voiceVadLock)
                    {
                        if (voiceVadSession != null)
                        {
                            try
                            {
                                voiceVadSession.Stop();
                            }
                            catch
                            {
                            }

                            voiceVadSession.SpeechSegmentReady -=
                                VoiceVadSession_SpeechSegmentReady;

                            voiceVadSession.Dispose();

                            voiceVadSession = null;
                        }


                        voiceVadSession =
                            new SileroVadSession();


                        voiceVadSession.SpeechSegmentReady +=
                            VoiceVadSession_SpeechSegmentReady;


                        voiceVadSession.Start(
                            voiceRecordingFormat);
                    }


                    Debug.WriteLine(
                        "SILERO VAD SESSION STARTED FOR NEW VOICE CYCLE");


                    // =================================================
                    // FEED PRE-BUFFER INTO VAD
                    //
                    // This is what prevents the first words from
                    // being lost by VAD.
                    // =================================================

                    byte[] preBuffer =
                        GetAutoVoicePreBufferSnapshot();


                    if (preBuffer != null &&
                        preBuffer.Length > 0)
                    {
                        lock (voiceVadLock)
                        {
                            if (voiceVadSession != null)
                            {
                                voiceVadSession.AcceptAudio(
                                    preBuffer,
                                    preBuffer.Length);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    voiceVadSession = null;

                    Debug.WriteLine(
                        "SILERO VAD START ERROR: " +
                        ex);
                }


                // =====================================================
                // QUESTION QUEUE
                //
                // EXISTING QUEUE LOGIC UNCHANGED.
                // =====================================================

                StartVoiceQuestionQueue();


                // =====================================================
                // NOW VOICE CYCLE IS ACTIVE
                // =====================================================

                isVoiceRecording = true;


                // =====================================================
                // UI
                // =====================================================

                SetVoiceInputMode(true);

                CreateLiveVoiceMessage();

                UpdateLiveVoiceMessage(
                    "Listening...");


                if (VoiceButton != null)
                {
                    VoiceButton.Background =
                        new SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(
                                190,
                                45,
                                45));
                }


                // =====================================================
                // PULSE
                // =====================================================

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
                    "VOICE CYCLE STARTED");

                Debug.WriteLine(
                    "CONTINUOUS CAPTURE REMAINS RUNNING");
            }
            catch (Exception ex)
            {
                isVoiceRecording = false;

                voiceStopping = false;

                Debug.WriteLine(
                    "VOICE CYCLE START ERROR: " +
                    ex);

                throw;
            }
        }

        // =========================================================
        // INITIALIZE ROLLING PRE-BUFFER
        // =========================================================

        private void InitializeAutoVoicePreBuffer(
            WaveFormat format)
        {
            if (format == null)
                return;


            int bytesPerSecond =
                Math.Max(
                    1,
                    format.AverageBytesPerSecond);


            int bufferSize =
                (int)(
                    bytesPerSecond *
                    AutoVoicePreBufferSeconds);


            // Keep enough room for the requested duration.
            bufferSize =
                Math.Max(
                    bufferSize,
                    format.BlockAlign * 1024);


            lock (autoVoicePreBufferLock)
            {
                autoVoicePreBuffer =
                    new byte[bufferSize];

                autoVoicePreBufferWritePosition =
                    0;

                autoVoicePreBufferCount =
                    0;
            }


            Debug.WriteLine(
                "AUTO VOICE PRE-BUFFER = " +
                AutoVoicePreBufferSeconds +
                " sec | " +
                bufferSize +
                " bytes");
        }


        // =========================================================
        // APPEND AUDIO TO ROLLING PRE-BUFFER
        // =========================================================

        private void AppendToAutoVoicePreBuffer(
            byte[] buffer,
            int bytesRecorded)
        {
            if (buffer == null ||
                bytesRecorded <= 0)
            {
                return;
            }


            lock (autoVoicePreBufferLock)
            {
                if (autoVoicePreBuffer == null ||
                    autoVoicePreBuffer.Length == 0)
                {
                    return;
                }


                int remaining =
                    bytesRecorded;

                int sourceOffset =
                    0;


                while (remaining > 0)
                {
                    int copyLength =
                        Math.Min(
                            remaining,
                            autoVoicePreBuffer.Length);


                    int firstPart =
                        Math.Min(
                            copyLength,
                            autoVoicePreBuffer.Length -
                            autoVoicePreBufferWritePosition);


                    Buffer.BlockCopy(
                        buffer,
                        sourceOffset,
                        autoVoicePreBuffer,
                        autoVoicePreBufferWritePosition,
                        firstPart);


                    int secondPart =
                        copyLength -
                        firstPart;


                    if (secondPart > 0)
                    {
                        Buffer.BlockCopy(
                            buffer,
                            sourceOffset +
                            firstPart,
                            autoVoicePreBuffer,
                            0,
                            secondPart);
                    }


                    autoVoicePreBufferWritePosition =
                        (
                            autoVoicePreBufferWritePosition +
                            copyLength
                        ) %
                        autoVoicePreBuffer.Length;


                    autoVoicePreBufferCount =
                        Math.Min(
                            autoVoicePreBufferCount +
                            copyLength,
                            autoVoicePreBuffer.Length);


                    sourceOffset +=
                        copyLength;

                    remaining -=
                        copyLength;
                }
            }
        }


        // =========================================================
        // GET PRE-BUFFER SNAPSHOT
        // =========================================================

        private byte[] GetAutoVoicePreBufferSnapshot()
        {
            lock (autoVoicePreBufferLock)
            {
                if (autoVoicePreBuffer == null ||
                    autoVoicePreBufferCount <= 0)
                {
                    return null;
                }


                byte[] result =
                    new byte[
                        autoVoicePreBufferCount];


                int start =
                    autoVoicePreBufferWritePosition -
                    autoVoicePreBufferCount;


                if (start < 0)
                {
                    start +=
                        autoVoicePreBuffer.Length;
                }


                int firstPart =
                    Math.Min(
                        autoVoicePreBufferCount,
                        autoVoicePreBuffer.Length -
                        start);


                Buffer.BlockCopy(
                    autoVoicePreBuffer,
                    start,
                    result,
                    0,
                    firstPart);


                int secondPart =
                    autoVoicePreBufferCount -
                    firstPart;


                if (secondPart > 0)
                {
                    Buffer.BlockCopy(
                        autoVoicePreBuffer,
                        0,
                        result,
                        firstPart,
                        secondPart);
                }


                return result;
            }
        }


        // =========================================================
        // CLEAR PRE-BUFFER
        // =========================================================

        private void ClearAutoVoicePreBuffer()
        {
            lock (autoVoicePreBufferLock)
            {
                autoVoicePreBuffer = null;

                autoVoicePreBufferWritePosition =
                    0;

                autoVoicePreBufferCount =
                    0;
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

                // =========================================================
                // ALWAYS MAINTAIN ROLLING PRE-BUFFER
                //
                // This runs even when Voice Cycle is OFF.
                // =========================================================

                if (e.Buffer != null &&
                    e.BytesRecorded > 0)
                {
                    AppendToAutoVoicePreBuffer(
                        e.Buffer,
                        e.BytesRecorded);
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


                // =========================================================
                // AUTO VOICE DETECTION
                // =========================================================

                if (_autoVoiceManager != null &&
                    e.Buffer != null &&
                    e.BytesRecorded > 0 &&
                    voiceRecordingFormat != null)
                {
                    try
                    {
                        _autoVoiceManager.ProcessAudio(
                            e.Buffer,
                            e.BytesRecorded,
                            voiceRecordingFormat);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "AUTO VOICE AUDIO PROCESS ERROR: " + ex);
                    }
                }

                // =========================================================
                // CONTINUOUS SILERO VAD
                // =========================================================

                if (isVoiceRecording &&
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
            // NO ACTIVE VOICE CYCLE
            // =========================================================

            if (!isVoiceRecording)
            {
                return;
            }


            if (voiceStopping)
            {
                return;
            }


            // =========================================================
            // AUTO VOICE / CONTINUOUS CAPTURE
            //
            // Stop ONLY current voice cycle.
            //
            // DO NOT stop WasapiLoopbackCapture.
            // =========================================================

            if (_autoVoiceManager != null &&
                voiceRecorder != null)
            {
                FinalizeVoiceCycleOnly();

                return;
            }


            // =========================================================
            // NORMAL MANUAL MODE
            //
            // Existing capture-stop behavior.
            // =========================================================

            voiceStopping = true;

            isVoiceRecording = true;


            ResetVoiceUI();

            UpdateLiveVoiceMessage(
                "Processing...");


            try
            {
                if (localVoiceRecorder != null &&
                    localVoiceRecorder.IsRecording)
                {
                    try
                    {
                        localVoiceRecorder.Stop();
                    }
                    catch
                    {
                        localVoiceStopped = true;
                    }
                }
                else
                {
                    localVoiceStopped = true;
                }


                if (voiceRecorder != null)
                {
                    voiceRecorder.StopRecording();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Stop voice error: " +
                    ex);


                voiceStopping = false;

                isVoiceRecording = false;

                ResetVoiceUI();

                RemoveLiveVoiceMessage();
            }
        }

        // =========================================================
        // FINALIZE CURRENT VOICE CYCLE ONLY
        //
        // IMPORTANT:
        //
        // WasapiLoopbackCapture continues running.
        //
        // Only the current question/voice cycle is closed.
        // =========================================================

        private void FinalizeVoiceCycleOnly()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        FinalizeVoiceCycleOnly();
                    }));

                return;
            }


            if (!isVoiceRecording)
            {
                return;
            }


            if (voiceStopping)
            {
                return;
            }


            voiceStopping = true;


            Debug.WriteLine(
                "VOICE CYCLE: FINALIZING");


            // =========================================================
            // UI OFF
            // =========================================================

            ResetVoiceUI();

            UpdateLiveVoiceMessage(
                "Processing...");


            try
            {
                // =====================================================
                // STOP / FLUSH CURRENT VAD
                //
                // This produces the final speech segment including
                // the silence tail that occurred before detection.
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
                        "SILERO VAD CYCLE STOP ERROR: " +
                        ex);
                }


                // =================================================
                // BUILD FINAL QUESTION AUDIO
                // =================================================

                byte[] sessionWav =
                    BuildVoiceSessionWav();


                if (sessionWav != null &&
                    sessionWav.Length > 44)
                {
                    Debug.WriteLine(
                        "VOICE SESSION WAV READY | SIZE = " +
                        sessionWav.Length);


                    // =================================================
                    // EXISTING QUEUE
                    //
                    // DO NOT CHANGE QUEUE IMPLEMENTATION.
                    // =================================================

                    EnqueueVoiceSession(
                        sessionWav);
                }
                else
                {
                    Debug.WriteLine(
                        "VOICE SESSION: NO SPEECH DETECTED");
                }


                RemoveLiveVoiceMessage();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE CYCLE FINALIZE ERROR: " +
                    ex);


                RemoveLiveVoiceMessage();
            }
            finally
            {
                // =====================================================
                // CURRENT CYCLE CLOSED
                //
                // CAPTURE IS STILL RUNNING.
                // =====================================================

                isVoiceRecording = false;

                voiceStopping = false;


                // =====================================================
                // DO NOT CLEAR voiceRecordingFormat
                //
                // Continuous capture still needs it.
                // =====================================================

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


                Debug.WriteLine(
                    "VOICE CYCLE: COMPLETED");

                Debug.WriteLine(
                    "VOICE CYCLE: CONTINUOUS CAPTURE STILL RUNNING");
            }
        }

        // =========================================================
        // STOP CONTINUOUS SYSTEM AUDIO CAPTURE
        //
        // This is called only when Auto Voice is turned OFF
        // or application is shutting down.
        //
        // This is the ONLY place where continuous Auto Voice
        // capture is stopped.
        // =========================================================

        private void StopContinuousVoiceCapture()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        StopContinuousVoiceCapture();
                    }));

                return;
            }


            WasapiLoopbackCapture recorder =
                voiceRecorder;


            if (recorder == null)
            {
                ClearAutoVoicePreBuffer();

                return;
            }


            Debug.WriteLine(
                "CONTINUOUS CAPTURE: STOP REQUESTED");


            try
            {
                recorder.StopRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "CONTINUOUS CAPTURE STOP ERROR: " +
                    ex);
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

                ClearAutoVoicePreBuffer();

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