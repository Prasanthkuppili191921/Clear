using System;
using System.Diagnostics;
using System.Windows;

namespace AiInterviewAssistant.AutoVoice
{
    public sealed class AutoVoiceManager : IDisposable
    {
        private readonly System.Windows.Threading.Dispatcher dispatcher;

        private readonly Func<bool> isVoiceRecording;
        private readonly Func<bool> isVoiceStopping;

        private readonly Action prepareVoiceCycle;
        private readonly Action startVoiceCycle;
        private readonly Action stopVoiceCycle;

        private readonly Action startVoiceCapture;
        private readonly Action stopVoiceCapture;

        private bool autoVoiceEnabled;
        private bool disposed;

        private bool cycleStartPending;

        private DateTime lastAudioDetectedUtc =
            DateTime.MinValue;

        private const int SilenceTimeoutMs = 850;

        private const float RmsThreshold = 0.005f;
        private const float PeakThreshold = 0.010f;


        public AutoVoiceManager(
            System.Windows.Threading.Dispatcher dispatcher,
            Func<bool> isVoiceRecording,
            Func<bool> isVoiceStopping,
            Action prepareVoiceCycle,
            Action startVoiceCycle,
            Action stopVoiceCycle,
            Action startVoiceCapture,
            Action stopVoiceCapture)
        {
            this.dispatcher = dispatcher;

            this.isVoiceRecording =
                isVoiceRecording;

            this.isVoiceStopping =
                isVoiceStopping;

            this.prepareVoiceCycle =
                prepareVoiceCycle;

            this.startVoiceCycle =
                startVoiceCycle;

            this.stopVoiceCycle =
                stopVoiceCycle;

            this.startVoiceCapture =
                startVoiceCapture;

            this.stopVoiceCapture =
                stopVoiceCapture;
        }


        // =========================================================
        // ENABLE AUTO VOICE
        // =========================================================

        public void Enable()
        {
            if (disposed)
                return;

            autoVoiceEnabled = true;

            cycleStartPending = false;

            lastAudioDetectedUtc =
                DateTime.MinValue;

            Debug.WriteLine(
                "AUTO VOICE: ON");


            dispatcher.BeginInvoke(
                new Action(() =>
                {
                    try
                    {
                        if (disposed ||
                            !autoVoiceEnabled)
                        {
                            return;
                        }

                        Debug.WriteLine(
                            "AUTO VOICE: STARTING CONTINUOUS CAPTURE");

                        // IMPORTANT:
                        //
                        // This starts ONLY WasapiLoopbackCapture.
                        //
                        // It does NOT start a voice cycle.
                        // Voice icon must remain OFF until
                        // actual system audio is detected.

                        startVoiceCapture();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "AUTO VOICE: CAPTURE START ERROR: " +
                            ex);
                    }
                }));
        }


        // =========================================================
        // DISABLE AUTO VOICE
        // =========================================================

        public void Disable()
        {
            if (disposed)
                return;

            autoVoiceEnabled = false;

            cycleStartPending = false;

            lastAudioDetectedUtc =
                DateTime.MinValue;


            Debug.WriteLine(
                "AUTO VOICE: OFF");


            dispatcher.BeginInvoke(
                new Action(() =>
                {
                    try
                    {
                        // -----------------------------------------
                        // FINISH ACTIVE VOICE CYCLE
                        // -----------------------------------------

                        if (isVoiceRecording() &&
                            !isVoiceStopping())
                        {
                            Debug.WriteLine(
                                "AUTO VOICE: OFF -> FINISH CURRENT CYCLE");

                            stopVoiceCycle();
                        }


                        // -----------------------------------------
                        // STOP CONTINUOUS CAPTURE
                        // -----------------------------------------

                        Debug.WriteLine(
                            "AUTO VOICE: OFF -> STOP CONTINUOUS CAPTURE");

                        stopVoiceCapture();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "AUTO VOICE: STOP ERROR: " +
                            ex);
                    }
                }));
        }


        // =========================================================
        // AUDIO DATA
        // =========================================================

        public void ProcessAudio(
            byte[] buffer,
            int bytesRecorded,
            NAudio.Wave.WaveFormat format)
        {
            if (disposed ||
                !autoVoiceEnabled)
            {
                return;
            }

            if (buffer == null ||
                bytesRecorded <= 0 ||
                format == null)
            {
                return;
            }


            float level;

            try
            {
                level =
                    CalculateAudioLevel(
                        buffer,
                        bytesRecorded,
                        format);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "AUTO VOICE: AUDIO LEVEL ERROR: " +
                    ex);

                return;
            }


            //bool audioDetected =
            //    level >= RmsThreshold ||
            //    level >= PeakThreshold;

            bool audioDetected =
                level >= RmsThreshold;


            // =====================================================
            // SYSTEM VOICE DETECTED
            // =====================================================

            if (audioDetected)
            {
                lastAudioDetectedUtc =
                    DateTime.UtcNow;


                // ---------------------------------------------
                // Already recording this voice cycle
                // ---------------------------------------------

                if (isVoiceRecording())
                {
                    return;
                }


                // ---------------------------------------------
                // Start request already queued
                // ---------------------------------------------

                if (cycleStartPending)
                {
                    return;
                }


                cycleStartPending = true;


                Debug.WriteLine(
                    "AUTO VOICE: SYSTEM AUDIO DETECTED");


                dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        try
                        {
                            if (disposed ||
                                !autoVoiceEnabled)
                            {
                                return;
                            }

                            if (isVoiceRecording())
                            {
                                return;
                            }

                            if (isVoiceStopping())
                            {
                                return;
                            }


                            Debug.WriteLine(
                                "AUTO VOICE: STARTING VOICE CYCLE");


                            prepareVoiceCycle();

                            startVoiceCycle();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(
                                "AUTO VOICE: VOICE CYCLE START ERROR: " +
                                ex);
                        }
                        finally
                        {
                            cycleStartPending = false;
                        }
                    }));


                return;
            }


            // =====================================================
            // SILENCE
            // =====================================================

            TryStopVoice();
        }


        // =========================================================
        // SILENCE CHECK
        // =========================================================

        private void TryStopVoice()
        {
            if (disposed ||
                !autoVoiceEnabled)
            {
                return;
            }

            if (!isVoiceRecording())
            {
                return;
            }

            if (isVoiceStopping())
            {
                return;
            }

            if (lastAudioDetectedUtc ==
                DateTime.MinValue)
            {
                return;
            }


            double silenceMs =
                (
                    DateTime.UtcNow -
                    lastAudioDetectedUtc
                ).TotalMilliseconds;


            if (silenceMs <
                SilenceTimeoutMs)
            {
                return;
            }


            dispatcher.BeginInvoke(
                new Action(() =>
                {
                    try
                    {
                        if (disposed ||
                            !autoVoiceEnabled)
                        {
                            return;
                        }

                        if (!isVoiceRecording())
                        {
                            return;
                        }

                        if (isVoiceStopping())
                        {
                            return;
                        }


                        double currentSilenceMs =
                            (
                                DateTime.UtcNow -
                                lastAudioDetectedUtc
                            ).TotalMilliseconds;


                        if (currentSilenceMs <
                            SilenceTimeoutMs)
                        {
                            return;
                        }


                        Debug.WriteLine(
                            "AUTO VOICE: SYSTEM AUDIO ENDED");


                        Debug.WriteLine(
                            "AUTO VOICE: FINISHING VOICE CYCLE");


                        stopVoiceCycle();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "AUTO VOICE: CYCLE STOP ERROR: " +
                            ex);
                    }
                }));
        }


        // =========================================================
        // AUDIO LEVEL
        // =========================================================

        private float CalculateAudioLevel(
            byte[] buffer,
            int bytesRecorded,
            NAudio.Wave.WaveFormat format)
        {
            if (format.Encoding ==
                NAudio.Wave.WaveFormatEncoding.IeeeFloat &&
                format.BitsPerSample == 32)
            {
                int sampleCount =
                    bytesRecorded / 4;

                if (sampleCount <= 0)
                    return 0f;


                double sumSquares = 0.0;

                float peak = 0f;


                for (int i = 0;
                     i < sampleCount;
                     i++)
                {
                    int offset =
                        i * 4;

                    if (offset + 3 >=
                        bytesRecorded)
                    {
                        break;
                    }


                    float sample =
                        BitConverter.ToSingle(
                            buffer,
                            offset);


                    float abs =
                        Math.Abs(sample);


                    if (abs > peak)
                    {
                        peak = abs;
                    }


                    sumSquares +=
                        sample * sample;
                }


                float rms =
                    (float)Math.Sqrt(
                        sumSquares /
                        sampleCount);


                return Math.Max(
                    rms,
                    peak);
            }


            if (format.Encoding ==
                NAudio.Wave.WaveFormatEncoding.Pcm &&
                format.BitsPerSample == 16)
            {
                int sampleCount =
                    bytesRecorded / 2;

                if (sampleCount <= 0)
                    return 0f;


                double sumSquares = 0.0;

                float peak = 0f;


                for (int i = 0;
                     i < sampleCount;
                     i++)
                {
                    int offset =
                        i * 2;

                    if (offset + 1 >=
                        bytesRecorded)
                    {
                        break;
                    }


                    short value =
                        BitConverter.ToInt16(
                            buffer,
                            offset);


                    float sample =
                        value / 32768f;


                    float abs =
                        Math.Abs(sample);


                    if (abs > peak)
                    {
                        peak = abs;
                    }


                    sumSquares +=
                        sample * sample;
                }


                float rms =
                    (float)Math.Sqrt(
                        sumSquares /
                        sampleCount);


                return Math.Max(
                    rms,
                    peak);
            }


            return 0f;
        }


        // =========================================================
        // DISPOSE
        // =========================================================

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            autoVoiceEnabled = false;

            cycleStartPending = false;

            Debug.WriteLine(
                "AUTO VOICE: DISPOSED");
        }
    }
}