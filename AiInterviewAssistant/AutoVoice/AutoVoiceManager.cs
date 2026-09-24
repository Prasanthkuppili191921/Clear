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

        private DateTime lastSpeechDetectedUtc =
            DateTime.MinValue;

        private DateTime speechStartedUtc =
            DateTime.MinValue;

        private const int SpeechConfirmationMs = 250;
        private const int SilenceTimeoutMs = 1000;

        private SileroVadSession autoVoiceVadSession;

        private bool vadStarted;


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

            this.isVoiceRecording = isVoiceRecording;
            this.isVoiceStopping = isVoiceStopping;

            this.prepareVoiceCycle = prepareVoiceCycle;
            this.startVoiceCycle = startVoiceCycle;
            this.stopVoiceCycle = stopVoiceCycle;

            this.startVoiceCapture = startVoiceCapture;
            this.stopVoiceCapture = stopVoiceCapture;
        }


        public void Enable()
        {
            if (disposed)
                return;

            autoVoiceEnabled = true;

            cycleStartPending = false;

            lastSpeechDetectedUtc =
                DateTime.MinValue;

            speechStartedUtc =
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


        public void Disable()
        {
            if (disposed)
                return;

            autoVoiceEnabled = false;

            cycleStartPending = false;

            lastSpeechDetectedUtc =
                DateTime.MinValue;

            speechStartedUtc =
                DateTime.MinValue;


            Debug.WriteLine(
                "AUTO VOICE: OFF");


            dispatcher.BeginInvoke(
                new Action(() =>
                {
                    try
                    {
                        if (isVoiceRecording() &&
                            !isVoiceStopping())
                        {
                            Debug.WriteLine(
                                "AUTO VOICE: OFF -> FINISH CURRENT CYCLE");

                            stopVoiceCycle();
                        }


                        StopVad();


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


            try
            {
                /*
                 * Start Silero VAD once when continuous
                 * system-audio capture starts.
                 */
                if (!vadStarted)
                {
                    StartVad(format);
                }


                if (autoVoiceVadSession == null)
                    return;


                /*
                 * Feed raw WASAPI loopback audio directly
                 * into Silero VAD.
                 */
                autoVoiceVadSession.AcceptAudio(
                    buffer,
                    bytesRecorded);


                /*
                 * IMPORTANT:
                 *
                 * IsSpeechDetected() is the LIVE speech
                 * state exposed by SherpaOnnx.
                 *
                 * Do NOT use IsEmpty() here.
                 *
                 * IsEmpty() only tells us whether a
                 * completed speech segment is queued.
                 */
                if (autoVoiceVadSession.IsSpeechDetected)
                {
                    ProcessSpeechDetected();
                }
                else
                {
                    TryStopVoice();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "AUTO VOICE: AUDIO PROCESSING ERROR: " +
                    ex);
            }
        }


        private void StartVad(
            NAudio.Wave.WaveFormat format)
        {
            if (disposed ||
                !autoVoiceEnabled ||
                vadStarted)
            {
                return;
            }


            try
            {
                StopVad();


                autoVoiceVadSession =
                    new SileroVadSession();


                autoVoiceVadSession.Start(
                    format);


                vadStarted = true;


                Debug.WriteLine(
                    "AUTO VOICE: SILERO VAD STARTED");
            }
            catch (Exception ex)
            {
                vadStarted = false;


                Debug.WriteLine(
                    "AUTO VOICE: SILERO VAD START ERROR: " +
                    ex);


                if (autoVoiceVadSession != null)
                {
                    try
                    {
                        autoVoiceVadSession.Dispose();
                    }
                    catch
                    {
                    }


                    autoVoiceVadSession = null;
                }
            }
        }


        private void ProcessSpeechDetected()
        {
            if (disposed ||
                !autoVoiceEnabled)
            {
                return;
            }


            DateTime nowUtc =
                DateTime.UtcNow;


            /*
             * Every confirmed/live speech chunk refreshes
             * the last speech time.
             */
            lastSpeechDetectedUtc =
                nowUtc;


            /*
             * If a voice cycle is already recording,
             * do not start another cycle.
             */
            if (isVoiceRecording())
            {
                speechStartedUtc =
                    DateTime.MinValue;

                return;
            }


            /*
             * A start request is already waiting on the
             * Dispatcher.
             */
            if (cycleStartPending)
                return;


            /*
             * First speech detection.
             *
             * We intentionally wait for the configured
             * confirmation duration before starting the
             * voice cycle.
             */
            if (speechStartedUtc ==
                DateTime.MinValue)
            {
                speechStartedUtc =
                    nowUtc;


                Debug.WriteLine(
                    "AUTO VOICE: SILERO SPEECH DETECTED - WAITING FOR CONFIRMATION");

                return;
            }


            double speechMs =
                (nowUtc - speechStartedUtc)
                .TotalMilliseconds;


            if (speechMs <
                SpeechConfirmationMs)
            {
                return;
            }


            cycleStartPending = true;


            Debug.WriteLine(
                "AUTO VOICE: SPEECH CONFIRMED - " +
                speechMs.ToString("0") +
                " ms");


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
                            return;


                        if (isVoiceStopping())
                            return;


                        Debug.WriteLine(
                            "AUTO VOICE: STARTING VOICE CYCLE");


                        /*
                         * IMPORTANT:
                         *
                         * Existing Voice Cycle logic is
                         * preserved.
                         */
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
                        cycleStartPending =
                            false;
                    }
                }));
        }


        private void TryStopVoice()
        {
            if (disposed ||
                !autoVoiceEnabled)
            {
                return;
            }


            /*
             * Nothing to stop.
             */
            if (!isVoiceRecording())
                return;


            if (isVoiceStopping())
                return;


            /*
             * No speech has ever been detected.
             */
            if (lastSpeechDetectedUtc ==
                DateTime.MinValue)
            {
                return;
            }


            double silenceMs =
                (DateTime.UtcNow -
                 lastSpeechDetectedUtc)
                .TotalMilliseconds;


            /*
             * Keep the voice cycle alive during the
             * configured silence timeout.
             */
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
                            return;


                        if (isVoiceStopping())
                            return;


                        /*
                         * Re-check silence on the UI
                         * thread before actually stopping.
                         */
                        double currentSilenceMs =
                            (DateTime.UtcNow -
                             lastSpeechDetectedUtc)
                            .TotalMilliseconds;


                        if (currentSilenceMs <
                            SilenceTimeoutMs)
                        {
                            return;
                        }


                        Debug.WriteLine(
                            "AUTO VOICE: SYSTEM AUDIO ENDED");


                        Debug.WriteLine(
                            "AUTO VOICE: FINISHING VOICE CYCLE");


                        speechStartedUtc =
                            DateTime.MinValue;

                        lastSpeechDetectedUtc =
                            DateTime.MinValue;


                        /*
                         * Existing Voice Cycle stop
                         * logic is preserved.
                         */
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


        private void StopVad()
        {
            try
            {
                if (autoVoiceVadSession != null)
                {
                    autoVoiceVadSession.Stop();

                    autoVoiceVadSession.Dispose();

                    autoVoiceVadSession =
                        null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "AUTO VOICE: VAD STOP ERROR: " +
                    ex);

                autoVoiceVadSession =
                    null;
            }
            finally
            {
                vadStarted = false;
            }
        }


        public void Dispose()
        {
            if (disposed)
                return;


            disposed = true;

            autoVoiceEnabled = false;

            cycleStartPending = false;

            speechStartedUtc =
                DateTime.MinValue;

            lastSpeechDetectedUtc =
                DateTime.MinValue;


            StopVad();


            Debug.WriteLine(
                "AUTO VOICE: DISPOSED");
        }
    }
}