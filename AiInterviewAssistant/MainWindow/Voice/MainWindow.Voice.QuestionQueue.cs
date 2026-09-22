using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // VOICE CYCLE
        // =========================================================

        private enum VoiceCycleState
        {
            Idle,
            SttProcessing,
            AiGenerating,
            Completed
        }

        private readonly object voiceCycleLock =
            new object();

        private VoiceCycleState voiceCycleState =
            VoiceCycleState.Idle;


        // =========================================================
        // QUESTIONS ALREADY TRANSCRIBED
        // =========================================================

        private readonly List<string>
            voiceCycleQuestions =
            new List<string>();


        // =========================================================
        // AUDIO WAITING FOR STT
        // =========================================================

        private readonly List<byte[]>
            voiceCyclePendingAudio =
            new List<byte[]>();


        // =========================================================
        // STT
        // =========================================================

        private byte[] voiceSttCurrentAudio;

        private CancellationTokenSource
            voiceSttCts;

        private Task
            voiceSttTask;


        // =========================================================
        // AI
        // =========================================================

        private CancellationTokenSource
            voiceAiCts;

        private Task
            voiceAiTask;

        private int voiceAiGenerationId = 0;

        private bool voiceAiRequestStarted = false;


        // =========================================================
        // TRUE WHEN THE CURRENT CYCLE MUST WAIT FOR ANOTHER
        // VOICE SESSION BEFORE AI CAN START.
        // =========================================================

        private bool voiceCycleWaitingForNextQuestion = false;


        // =========================================================
        // ONLY ONE PROCESSOR OWNS THE CURRENT CYCLE.
        // =========================================================

        private bool voiceCycleProcessorRunning = false;


        // =========================================================
        // START
        // =========================================================

        private void StartVoiceQuestionQueue()
        {
            Debug.WriteLine(
                "VOICE CYCLE: READY");
        }


        // =========================================================
        // VOICE ON
        // =========================================================

        private void PrepareVoiceCycleForNewRecording()
        {
            CancellationTokenSource aiCtsToCancel = null;

            lock (voiceCycleLock)
            {
                // =====================================================
                // PREVIOUS CYCLE COMPLETED
                // =====================================================

                if (voiceCycleState ==
                    VoiceCycleState.Completed)
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: PREVIOUS CYCLE COMPLETED");

                    voiceCycleQuestions.Clear();

                    voiceCyclePendingAudio.Clear();

                    voiceCycleWaitingForNextQuestion =
                        false;

                    voiceCycleState =
                        VoiceCycleState.Idle;
                }


                // =====================================================
                // STT ACTIVE
                //
                // NEVER CANCEL STT.
                // =====================================================

                bool sttIsActive =
                    voiceSttCts != null ||
                    (voiceSttTask != null &&
                     !voiceSttTask.IsCompleted);

                if (sttIsActive)
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: VOICE ON WHILE STT PROCESSING");

                    Debug.WriteLine(
                        "VOICE CYCLE: STT CONTINUES");

                    // -------------------------------------------------
                    // Current cycle now expects the new voice session
                    // as another question.
                    // -------------------------------------------------

                    voiceCycleWaitingForNextQuestion =
                        true;
                }


                // =====================================================
                // AI ACTIVE
                //
                // CANCEL ONLY AI.
                // =====================================================

                bool aiIsActive =
                    voiceAiCts != null &&
                    voiceAiTask != null &&
                    !voiceAiTask.IsCompleted;

                if (aiIsActive)
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: VOICE ON WHILE AI GENERATING");

                    Debug.WriteLine(
                        "VOICE CYCLE: CANCELLING AI ONLY");

                    aiCtsToCancel =
                        voiceAiCts;

                    // -------------------------------------------------
                    // Keep existing questions.
                    // -------------------------------------------------

                    voiceCycleWaitingForNextQuestion =
                        true;

                    voiceCycleState =
                        VoiceCycleState.Idle;
                }


                // =====================================================
                // CURRENT CYCLE REMAINS OPEN.
                // =====================================================

                if (voiceCycleState ==
                    VoiceCycleState.Idle)
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: NEW VOICE RECORDING");

                    Debug.WriteLine(
                        "VOICE CYCLE QUESTION COUNT = " +
                        voiceCycleQuestions.Count);

                    Debug.WriteLine(
                        "VOICE CYCLE PENDING AUDIO COUNT = " +
                        voiceCyclePendingAudio.Count);
                }
            }


            // =========================================================
            // CANCEL ONLY AI OUTSIDE LOCK
            // =========================================================

            if (aiCtsToCancel != null)
            {
                try
                {
                    aiCtsToCancel.Cancel();
                }
                catch
                {
                }
            }
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

                Debug.WriteLine(
                    "VOICE CYCLE: SESSION RECEIVED | SIZE = " +
                    sessionWav.Length);

                // =========================================================
                // THE QUESTION WE WERE WAITING FOR HAS NOW ARRIVED.
                //
                // Voice ON during STT/AI sets this flag to true.
                // Once the new session WAV is actually available,
                // clear it so the current cycle can continue:
                //
                // STT -> COMBINE QUESTIONS -> ONE AI REQUEST
                // =========================================================

                lock (voiceCycleLock)
                {
                    voiceCycleWaitingForNextQuestion =
                        false;

                    Debug.WriteLine(
                        "VOICE CYCLE: NEXT QUESTION SESSION RECEIVED");
                }


                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        try
                        {
                            RemoveLiveVoiceMessage();
                        }
                        catch (Exception uiEx)
                        {
                            Debug.WriteLine(
                                "VOICE CYCLE PROCESSING UI ERROR: " +
                                uiEx.Message);
                        }
                    }));


                _ = ProcessVoiceCycleAudioAsync(
                    sessionWav);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE CYCLE ENQUEUE ERROR: " +
                    ex);
            }
        }


        // =========================================================
        // PROCESS CURRENT VOICE SESSION
        // =========================================================

        private async Task ProcessVoiceCycleAudioAsync(
            byte[] newSessionWav)
        {
            // =====================================================
            // ONLY ONE PROCESSOR.
            //
            // If another session arrives while this processor is
            // active, its audio is stored in pendingAudio.
            //
            // IMPORTANT:
            // We DO NOT return from the active processor because
            // the active processor is responsible for continuing
            // the current cycle.
            // =====================================================

            bool becomeProcessor = false;

            lock (voiceCycleLock)
            {
                if (!voiceCycleProcessorRunning)
                {
                    voiceCycleProcessorRunning =
                        true;

                    becomeProcessor = true;
                }
                else
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: PROCESSOR ALREADY RUNNING");

                    if (newSessionWav != null &&
                        newSessionWav.Length > 44)
                    {
                        voiceCyclePendingAudio.Add(
                            newSessionWav);

                        Debug.WriteLine(
                            "VOICE CYCLE: NEW SESSION ADDED TO PENDING AUDIO");

                        Debug.WriteLine(
                            "VOICE CYCLE PENDING AUDIO COUNT = " +
                            voiceCyclePendingAudio.Count);
                    }

                    // -------------------------------------------------
                    // The currently running processor will continue
                    // the cycle.
                    // -------------------------------------------------

                    return;
                }
            }


            if (!becomeProcessor)
                return;


            try
            {
                // =================================================
                // PROCESS LOOP
                // =================================================

                while (true)
                {
                    List<byte[]> audioToProcess =
                        new List<byte[]>();


                    // =================================================
                    // TAKE CURRENT + PENDING AUDIO
                    // =================================================

                    lock (voiceCycleLock)
                    {
                        // -------------------------------------------------
                        // First process pending audio.
                        // -------------------------------------------------

                        if (voiceCyclePendingAudio.Count > 0)
                        {
                            audioToProcess.AddRange(
                                voiceCyclePendingAudio);

                            voiceCyclePendingAudio.Clear();
                        }


                        // -------------------------------------------------
                        // Then process the current session.
                        // -------------------------------------------------

                        if (newSessionWav != null &&
                            newSessionWav.Length > 44)
                        {
                            audioToProcess.Add(
                                newSessionWav);

                            newSessionWav = null;
                        }


                        // -------------------------------------------------
                        // We are now doing STT.
                        // -------------------------------------------------

                        voiceCycleState =
                            VoiceCycleState.SttProcessing;
                    }


                    // =================================================
                    // WAIT FOR EXISTING STT
                    //
                    // NEVER CANCEL IT.
                    // =================================================

                    Task oldSttTask;

                    lock (voiceCycleLock)
                    {
                        oldSttTask =
                            voiceSttTask;
                    }


                    if (oldSttTask != null)
                    {
                        try
                        {
                            await oldSttTask
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                    }


                    // =================================================
                    // TRANSCRIBE AUDIO
                    // =================================================

                    foreach (byte[] audio in audioToProcess)
                    {
                        if (audio == null ||
                            audio.Length <= 44)
                        {
                            continue;
                        }


                        string questionText =
                            await TranscribeVoiceCycleAudioAsync(
                                audio)
                                .ConfigureAwait(false);


                        // =================================================
                        // STT CANCELLED
                        // =================================================

                        if (questionText == null)
                        {
                            lock (voiceCycleLock)
                            {
                                if (!voiceCyclePendingAudio.Contains(
                                    audio))
                                {
                                    voiceCyclePendingAudio.Insert(
                                        0,
                                        audio);
                                }

                                voiceCycleState =
                                    VoiceCycleState.Idle;
                            }

                            Debug.WriteLine(
                                "VOICE CYCLE: STT CANCELLED - AUDIO RETAINED");

                            return;
                        }


                        // =================================================
                        // NO TEXT
                        // =================================================

                        if (string.IsNullOrWhiteSpace(
                            questionText))
                        {
                            continue;
                        }

                        questionText =
                            questionText.Trim();


                        lock (voiceCycleLock)
                        {
                            voiceCycleQuestions.Add(
                                questionText);

                            Debug.WriteLine(
                                "VOICE CYCLE: QUESTION ADDED");

                            Debug.WriteLine(
                                "VOICE CYCLE QUESTION COUNT = " +
                                voiceCycleQuestions.Count);
                        }
                    }


                    // =================================================
                    // CHECK FOR MORE AUDIO
                    //
                    // If Voice ON happened while STT/AI was active,
                    // current cycle must not go to AI until the new
                    // session is also available.
                    // =================================================

                    bool moreAudioAvailable;

                    bool waitingForNextQuestion;

                    bool sttStillRunning;

                    lock (voiceCycleLock)
                    {
                        moreAudioAvailable =
                            voiceCyclePendingAudio.Count > 0;

                        waitingForNextQuestion =
                            voiceCycleWaitingForNextQuestion;

                        sttStillRunning =
                            voiceSttCts != null ||
                            (voiceSttTask != null &&
                             !voiceSttTask.IsCompleted);
                    }


                    // =================================================
                    // STT STILL RUNNING
                    // =================================================

                    if (sttStillRunning)
                    {
                        Debug.WriteLine(
                            "VOICE CYCLE: WAITING FOR STT TO COMPLETE");

                        await Task.Yield();

                        continue;
                    }


                    // =================================================
                    // MORE AUDIO ALREADY ARRIVED
                    // =================================================

                    if (moreAudioAvailable)
                    {
                        Debug.WriteLine(
                            "VOICE CYCLE: PROCESSING PENDING AUDIO");

                        lock (voiceCycleLock)
                        {
                            voiceCycleWaitingForNextQuestion =
                                false;
                        }

                        continue;
                    }


                    // =================================================
                    // WAITING FOR A NEW VOICE SESSION
                    //
                    // At this point the current processor has already
                    // processed all available audio.
                    //
                    // We cannot start AI yet if Voice ON told us that
                    // another question belongs to this cycle.
                    //
                    // The processor is allowed to finish here.
                    // The next EnqueueVoiceSession() will start a new
                    // processor and existing questions are retained.
                    // =================================================

                    if (waitingForNextQuestion)
                    {
                        Debug.WriteLine(
                            "VOICE CYCLE: WAITING FOR NEXT QUESTION");

                        lock (voiceCycleLock)
                        {
                            voiceCycleState =
                                VoiceCycleState.Idle;
                        }

                        return;
                    }


                    // =================================================
                    // GET COMPLETE CURRENT-CYCLE QUESTION
                    // =================================================

                    string combinedQuestion;

                    lock (voiceCycleLock)
                    {
                        if (voiceCycleQuestions.Count == 0)
                        {
                            voiceCycleState =
                                VoiceCycleState.Idle;

                            return;
                        }

                        combinedQuestion =
                            string.Join(
                                " ",
                                voiceCycleQuestions
                                    .Where(q => !string.IsNullOrWhiteSpace(q))
                                    .Select(q => q.Trim()));
                    }


                    Debug.WriteLine(
                        "VOICE CYCLE: COMBINED QUESTION READY");

                    Debug.WriteLine(
                        combinedQuestion);


                    // =================================================
                    // REMOVE PROCESSING MESSAGE
                    // =================================================

                    RemoveLiveVoiceMessage();


                    // =================================================
                    // START ONE AI REQUEST
                    // =================================================

                    Debug.WriteLine(
                        "VOICE CYCLE: AI GENERATION START");

                    Task aiTask =
                        StartVoiceCycleAiAsync(
                            combinedQuestion);

                    lock (voiceCycleLock)
                    {
                        voiceAiTask =
                            aiTask;
                    }


                    await aiTask
                        .ConfigureAwait(false);

                    return;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine(
                    "VOICE CYCLE: PROCESSING CANCELLED");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE CYCLE PROCESS ERROR: " +
                    ex);

                lock (voiceCycleLock)
                {
                    if (voiceCycleState !=
                        VoiceCycleState.AiGenerating)
                    {
                        voiceCycleState =
                            VoiceCycleState.Idle;
                    }
                }
            }
            finally
            {
                lock (voiceCycleLock)
                {
                    voiceCycleProcessorRunning =
                        false;
                }
            }
        }


        // =========================================================
        // STT
        // =========================================================

        private async Task<string>
            TranscribeVoiceCycleAudioAsync(
                byte[] audio)
        {
            CancellationTokenSource cts =
                new CancellationTokenSource();

            Task<string> currentTask = null;

            lock (voiceCycleLock)
            {
                voiceSttCts =
                    cts;

                voiceSttCurrentAudio =
                    audio;

                currentTask =
                    TranscribeWithOpenRouterAsync(
                        audio,
                        cts.Token);

                voiceSttTask =
                    currentTask;
            }

            try
            {
                Debug.WriteLine(
                    "VOICE CYCLE: STT START");

                string result =
                    await currentTask
                        .ConfigureAwait(false);

                if (cts.IsCancellationRequested)
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: STT CANCELLED");

                    return null;
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine(
                    "VOICE CYCLE: STT CANCELLED");

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE CYCLE STT ERROR: " +
                    ex);

                return string.Empty;
            }
            finally
            {
                lock (voiceCycleLock)
                {
                    if (ReferenceEquals(
                        voiceSttCts,
                        cts))
                    {
                        voiceSttCts = null;
                    }

                    if (ReferenceEquals(
                        voiceSttTask,
                        currentTask))
                    {
                        voiceSttTask = null;
                    }

                    if (ReferenceEquals(
                        voiceSttCurrentAudio,
                        audio))
                    {
                        voiceSttCurrentAudio = null;
                    }
                }

                cts.Dispose();
            }
        }


        // =========================================================
        // START AI
        // =========================================================

        private async Task StartVoiceCycleAiAsync(
            string combinedQuestion)
        {
            if (string.IsNullOrWhiteSpace(
                combinedQuestion))
            {
                return;
            }

            CancellationTokenSource cts =
                new CancellationTokenSource();

            int generationId;

            lock (voiceCycleLock)
            {
                generationId =
                    ++voiceAiGenerationId;

                voiceAiCts =
                    cts;

                voiceCycleState =
                    VoiceCycleState.AiGenerating;

                voiceAiRequestStarted =
                    false;
            }


            Border thinkingBubble = null;

            try
            {
                // =================================================
                // WAIT FOR PREVIOUS AI
                // =================================================

                Task previousAiTask = null;

                lock (voiceCycleLock)
                {
                    previousAiTask =
                        voiceAiTask;
                }


                if (previousAiTask != null)
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: WAITING FOR PREVIOUS AI TASK TO EXIT");

                    try
                    {
                        await previousAiTask
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                    }

                    Debug.WriteLine(
                        "VOICE CYCLE: PREVIOUS AI TASK EXITED");
                }


                // =================================================
                // CANCELLED WHILE WAITING
                // =================================================

                if (cts.IsCancellationRequested)
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: NEW AI REQUEST CANCELLED WHILE WAITING");

                    return;
                }


                // =================================================
                // UI
                // =================================================

                await Dispatcher.InvokeAsync(() =>
                {
                    RemoveLiveVoiceMessage();

                    AddUserMessage(
                        combinedQuestion);

                    thinkingBubble =
                        AddAIMessage("");

                    StartAITypingAnimation(
                        thinkingBubble,
                        "");
                });


                // =================================================
                // CANCELLED DURING UI
                // =================================================

                if (cts.IsCancellationRequested)
                {
                    Debug.WriteLine(
                        "VOICE CYCLE: AI CANCELLED BEFORE REQUEST START");

                    return;
                }


                // =================================================
                // MARK REQUEST STARTED
                // =================================================

                lock (voiceCycleLock)
                {
                    if (generationId ==
                        voiceAiGenerationId)
                    {
                        voiceAiRequestStarted =
                            true;
                    }
                }


                Debug.WriteLine(
                    "VOICE CYCLE: AI GENERATION START");


                // =================================================
                // SETTINGS
                // =================================================

                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();

                string answerMode =
                    settings.AnswerMode;

                if (string.IsNullOrWhiteSpace(answerMode))
                    answerMode = "Short";


                // =================================================
                // EXISTING AI PIPELINE
                // =================================================

                await AskOpenRouterStreaming(
                    combinedQuestion,
                    thinkingBubble,
                    cts.Token,
                    answerMode,
                    true)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine(
                    "VOICE CYCLE: AI GENERATION CANCELLED");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "VOICE CYCLE AI ERROR: " +
                    ex);
            }
            finally
            {
                bool wasCancelled =
                    cts.IsCancellationRequested;

                lock (voiceCycleLock)
                {
                    if (generationId ==
                        voiceAiGenerationId)
                    {
                        if (ReferenceEquals(
                            voiceAiCts,
                            cts))
                        {
                            voiceAiCts = null;
                        }


                        // =================================================
                        // AI CANCELLED
                        //
                        // QUESTIONS ARE RETAINED.
                        // =================================================

                        if (wasCancelled)
                        {
                            voiceCycleState =
                                VoiceCycleState.Idle;

                            Debug.WriteLine(
                                "VOICE CYCLE: AI CANCELLED - QUESTIONS RETAINED");
                        }
                        else
                        {
                            // =================================================
                            // SUCCESSFUL AI COMPLETION CLOSES THE CYCLE.
                            // =================================================

                            voiceCycleState =
                                VoiceCycleState.Completed;

                            voiceCycleQuestions.Clear();

                            voiceCyclePendingAudio.Clear();

                            voiceCycleWaitingForNextQuestion =
                                false;

                            Debug.WriteLine(
                                "VOICE CYCLE: COMPLETED AND CLOSED");
                        }
                    }
                }

                cts.Dispose();
            }
        }


        // =========================================================
        // STOP QUEUE
        // =========================================================

        private async Task StopVoiceQuestionQueueAsync()
        {
            CancellationTokenSource sttCts = null;
            CancellationTokenSource aiCts = null;

            Task sttTask = null;
            Task aiTask = null;

            lock (voiceCycleLock)
            {
                sttCts =
                    voiceSttCts;

                aiCts =
                    voiceAiCts;

                sttTask =
                    voiceSttTask;

                aiTask =
                    voiceAiTask;
            }


            try
            {
                sttCts?.Cancel();
            }
            catch
            {
            }

            try
            {
                aiCts?.Cancel();
            }
            catch
            {
            }


            if (sttTask != null)
            {
                try
                {
                    await sttTask
                        .ConfigureAwait(false);
                }
                catch
                {
                }
            }


            if (aiTask != null)
            {
                try
                {
                    await aiTask
                        .ConfigureAwait(false);
                }
                catch
                {
                }
            }


            lock (voiceCycleLock)
            {
                voiceCycleQuestions.Clear();

                voiceCyclePendingAudio.Clear();

                voiceSttCurrentAudio = null;

                voiceSttCts = null;

                voiceSttTask = null;

                voiceAiCts = null;

                voiceAiTask = null;

                voiceCycleWaitingForNextQuestion =
                    false;

                voiceCycleProcessorRunning =
                    false;

                voiceCycleState =
                    VoiceCycleState.Idle;
            }
        }


        // =========================================================
        // BUILD VOICE SESSION WAV
        // =========================================================

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


        // =========================================================
        // CREATE PCM16 WAV
        // =========================================================

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
                (short)(
                    channels *
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