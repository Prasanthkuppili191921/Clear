using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // VOICE QUESTION QUEUE
        // =========================================================

        private sealed class VoiceQuestionItem
        {
            public byte[] AudioWav { get; }

            public VoiceQuestionItem(byte[] audioWav)
            {
                AudioWav = audioWav;
            }
        }

        private readonly ConcurrentQueue<VoiceQuestionItem>
            voiceQuestionQueue =
                new ConcurrentQueue<VoiceQuestionItem>();

        private readonly SemaphoreSlim
            voiceQuestionSignal =
                new SemaphoreSlim(0);

        private CancellationTokenSource
            voiceQuestionQueueCts;

        private Task
            voiceQuestionWorkerTask;

        private int
            voiceQuestionWorkerStarted = 0;

        // =========================================================
        // QUESTION QUEUE START
        // =========================================================

        private void StartVoiceQuestionQueue()
        {
            if (Interlocked.Exchange(
                    ref voiceQuestionWorkerStarted,
                    1) == 1)
            {
                return;
            }

            voiceQuestionQueueCts =
                new CancellationTokenSource();

            voiceQuestionWorkerTask =
                Task.Run(
                    ProcessVoiceQuestionQueueAsync);
        }

        // =========================================================
        // ENQUEUE VOICE SESSION
        // =========================================================

        private void EnqueueVoiceSession(
            byte[] sessionWav)
        {
            try
            {
                if (sessionWav == null ||
                    sessionWav.Length <= 44)
                {
                    return;
                }

                voiceQuestionQueue.Enqueue(
                    new VoiceQuestionItem(
                        sessionWav));

                voiceQuestionSignal.Release();

                Debug.WriteLine(
                    "VOICE QUEUE: SESSION ENQUEUED | QUEUE SIZE = " +
                    voiceQuestionQueue.Count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE QUEUE SESSION ENQUEUE ERROR: " +
                    ex);
            }
        }

        // =========================================================
        // QUEUE WORKER
        // =========================================================

        private async Task ProcessVoiceQuestionQueueAsync()
        {
            CancellationToken token =
                voiceQuestionQueueCts.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await voiceQuestionSignal.WaitAsync(
                        token);

                    while (
                        voiceQuestionQueue.TryDequeue(
                            out VoiceQuestionItem item))
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        await ProcessVoiceQuestionAsync(
                            item.AudioWav,
                            token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when Voice is stopped.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "VOICE QUEUE WORKER ERROR: " +
                    ex);
            }
            finally
            {
                Interlocked.Exchange(
                    ref voiceQuestionWorkerStarted,
                    0);
            }
        }

        // =========================================================
        // PROCESS ONE QUESTION
        // =========================================================

        private async Task ProcessVoiceQuestionAsync(
            byte[] questionWav,
            CancellationToken token)
        {
            try
            {
                if (questionWav == null ||
                    questionWav.Length <= 44)
                {
                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    "========================================");

                System.Diagnostics.Debug.WriteLine(
                    "VOICE QUEUE: STT START");

                // -------------------------------------------------
                // IMPORTANT:
                //
                // This ONLY transcribes the question.
                //
                // Voice recorder continues running independently.
                // -------------------------------------------------

                string questionText =
                    await TranscribeWithOpenRouterAsync(
                        questionWav);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(
                    questionText))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "VOICE QUEUE: EMPTY TRANSCRIPT");

                    return;
                }

                questionText =
                    questionText.Trim();

                System.Diagnostics.Debug.WriteLine(
                    "VOICE QUEUE: TRANSCRIPT = [" +
                    questionText +
                    "]");

                // -------------------------------------------------
                // SEND TO EXISTING AI PIPELINE
                // -------------------------------------------------

                await Dispatcher.InvokeAsync(
                    async () =>
                    {
                        await ProcessVoiceQuestionTextAsync(
                            questionText);
                    });

                System.Diagnostics.Debug.WriteLine(
                    "VOICE QUEUE: QUESTION COMPLETE");

                System.Diagnostics.Debug.WriteLine(
                    "========================================");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "VOICE QUEUE QUESTION ERROR: " +
                    ex);
            }
        }

        // =========================================================
        // EXISTING AI ENTRY POINT
        // =========================================================
        //
        // Keep this method small.
        //
        // We will connect this to the SAME method that the
        // existing Send button uses.
        //
        // Do NOT create a second AI implementation.
        // =========================================================

        private async Task ProcessVoiceQuestionTextAsync(
    string questionText)
        {
            if (string.IsNullOrWhiteSpace(
                questionText))
            {
                return;
            }

            try
            {
                Debug.WriteLine(
                    "VOICE QUESTION READY FOR AI: " +
                    questionText);

                Border thinkingBubble = null;

                // =====================================================
                // ADD QUESTION + AI BUBBLE
                // =====================================================

                await Dispatcher.InvokeAsync(() =>
                {
                    RemoveLiveVoiceMessage();

                    AddUserMessage(questionText);

                    thinkingBubble =
                        AddAIMessage("");
                });

                // =====================================================
                // SEND THROUGH EXISTING AI PIPELINE
                // =====================================================

                Debug.WriteLine(
                    "VOICE: SENDING COMPLETE TEXT TO EXISTING AI PIPELINE...");

                await SendQuestion(
                    questionText,
                    thinkingBubble);

                Debug.WriteLine(
                    "VOICE: EXISTING AI PIPELINE COMPLETED");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE QUESTION AI ERROR: " +
                    ex);
            }
        }

        // =========================================================
        // STOP QUESTION QUEUE
        // =========================================================

        private async Task StopVoiceQuestionQueueAsync()
        {
            try
            {
                if (voiceQuestionQueueCts != null)
                {
                    voiceQuestionQueueCts.Cancel();
                }

                // Release worker if it is waiting.
                try
                {
                    voiceQuestionSignal.Release();
                }
                catch
                {
                }

                if (voiceQuestionWorkerTask != null)
                {
                    try
                    {
                        await voiceQuestionWorkerTask;
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                voiceQuestionWorkerTask = null;

                if (voiceQuestionQueueCts != null)
                {
                    voiceQuestionQueueCts.Dispose();
                    voiceQuestionQueueCts = null;
                }

                // -------------------------------------------------
                // Clear pending questions when Voice is OFF.
                // -------------------------------------------------

                while (
                    voiceQuestionQueue.TryDequeue(
                        out _))
                {
                }

                Interlocked.Exchange(
                    ref voiceQuestionWorkerStarted,
                    0);
            }
        }

        private byte[] BuildVoiceSessionWav()
        {
            List<byte[]> segments;

            lock (voiceSessionLock)
            {
                if (voiceSessionSpeechSegments.Count == 0)
                {
                    return null;
                }

                segments =
                    new List<byte[]>(
                        voiceSessionSpeechSegments);

                voiceSessionSpeechSegments.Clear();
            }

            try
            {
                int totalPcmBytes = 0;

                foreach (byte[] wav in segments)
                {
                    if (wav == null ||
                        wav.Length <= 44)
                    {
                        continue;
                    }

                    totalPcmBytes +=
                        wav.Length - 44;
                }

                if (totalPcmBytes <= 0)
                {
                    return null;
                }

                byte[] combinedPcm =
                    new byte[totalPcmBytes];

                int offset = 0;

                foreach (byte[] wav in segments)
                {
                    if (wav == null ||
                        wav.Length <= 44)
                    {
                        continue;
                    }

                    int pcmLength =
                        wav.Length - 44;

                    Buffer.BlockCopy(
                        wav,
                        44,
                        combinedPcm,
                        offset,
                        pcmLength);

                    offset += pcmLength;
                }

                // 16 kHz / Mono / PCM16
                return CreatePcm16Wav(
                    combinedPcm,
                    16000,
                    1,
                    16);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE SESSION WAV BUILD ERROR: " +
                    ex);

                return null;
            }
        }

        private byte[] CreatePcm16Wav(
    byte[] pcm,
    int sampleRate,
    short channels,
    short bitsPerSample)
        {
            if (pcm == null)
                return null;

            int byteRate =
                sampleRate *
                channels *
                bitsPerSample / 8;

            short blockAlign =
                (short)(channels *
                        bitsPerSample / 8);

            int fileSize =
                36 + pcm.Length;

            using (var stream =
                   new System.IO.MemoryStream(
                       44 + pcm.Length))
            using (var writer =
                   new System.IO.BinaryWriter(stream))
            {
                writer.Write(
                    Encoding.ASCII.GetBytes("RIFF"));

                writer.Write(fileSize);

                writer.Write(
                    Encoding.ASCII.GetBytes("WAVE"));

                writer.Write(
                    Encoding.ASCII.GetBytes("fmt "));

                writer.Write(16);

                writer.Write((short)1);

                writer.Write(channels);

                writer.Write(sampleRate);

                writer.Write(byteRate);

                writer.Write(blockAlign);

                writer.Write(bitsPerSample);

                writer.Write(
                    Encoding.ASCII.GetBytes("data"));

                writer.Write(pcm.Length);

                writer.Write(pcm);

                writer.Flush();

                return stream.ToArray();
            }
        }
    }
}