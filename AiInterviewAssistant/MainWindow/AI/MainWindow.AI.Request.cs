using AiInterviewAssistant.Settings.Resume;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        private async Task AskOpenRouterStreaming(
    string question,
    Border thinkingBubble,
    CancellationToken cancellationToken,
    string answerMode,
    bool voiceCycleRequest = false)
        {
            // =====================================================
            // PREVENT DUPLICATE AI REQUESTS
            // =====================================================

            if (System.Threading.Interlocked.CompareExchange(
                    ref _aiRequestInProgress,
                    1,
                    0) != 0)
            {
                Debug.WriteLine(
                    "AI REQUEST BLOCKED: Another request is running.");

                return;
            }

            DateTime requestStart = DateTime.Now;
            DateTime? headersTime = null;
            DateTime? streamTime = null;
            bool firstTokenReceived = false;

            object voiceUserHistoryEntry = null;
            bool voiceHistoryCommitted = false;

            try
            {
                Debug.WriteLine(
                    "=================================================");

                Debug.WriteLine(
                    "AI TIMING: REQUEST START = " +
                    requestStart.ToString("HH:mm:ss.fff"));

                // =====================================================
                // LOAD SETTINGS
                // =====================================================

                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();

                // =====================================================
                // API KEY
                // =====================================================

                string apiKey =
                    settings.OpenRouterApiKey;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    await FinalizeAIMessageOnUI(
                        thinkingBubble,
                        "OpenRouter API key not found.");

                    return;
                }

                apiKey = apiKey.Trim();

                // =====================================================
                // MODEL
                // =====================================================

                string routerModel =
                    settings.AnswerModel;

                if (string.IsNullOrWhiteSpace(routerModel))
                {
                    routerModel =
                        ConfigurationManager
                            .AppSettings["OpenRouterModel"];
                }

                if (string.IsNullOrWhiteSpace(routerModel))
                {
                    await FinalizeAIMessageOnUI(
                        thinkingBubble,
                        "OpenRouter model is not configured.");

                    return;
                }

                routerModel =
                    routerModel.Trim();

                Debug.WriteLine(
                    "AI TIMING: MODEL = " +
                    routerModel);

                // =====================================================
                // TEMPERATURE
                // =====================================================

                double temperature =
                    settings.Temperature;

                if (temperature < 0)
                    temperature = 0;

                if (temperature > 2)
                    temperature = 2;

                // =====================================================
                // RESPONSE LENGTH
                // =====================================================

                string responseLength =
                        settings.ResponseLength;

                if (string.IsNullOrWhiteSpace(
                    responseLength))
                {
                    responseLength = "Medium";
                }

                // =====================================================
                // LANGUAGE
                // =====================================================

                string languageInstruction =
                    "Answer in natural professional English.";

                string modeInstruction =
                    BuildAnswerModeInstruction(
                         answerMode,
                         responseLength);

                string finalQuestion =
                    modeInstruction +
                    "\n\n" +
                    languageInstruction +
                    "\n\n" +
                    "Interview question:\n" +
                    question;

                // =====================================================
                // ONLINE TEST MODE
                // =====================================================

                if (isOnlineTestMode)
                {
                    finalQuestion =
                        "This is an online multiple-choice test question.\n\n" +

                        "Identify the correct option from the question " +
                        "and options provided.\n\n" +

                        "Return ONLY the correct option letter and its answer.\n" +

                        "Do not explain the answer.\n" +
                        "Do not repeat the question.\n" +
                        "Do not provide any additional text.\n\n" +

                        "Format exactly like:\n" +
                        "C) answer\n\n" +

                        "Question:\n" +
                        question;
                }

                // =====================================================
                // ADD USER MESSAGE TO HISTORY
                // =====================================================

                voiceUserHistoryEntry = new
                {
                    role = "user",
                    content = finalQuestion
                };

                conversationHistory.Add(
                    voiceUserHistoryEntry);

                // =====================================================
                // TIMEOUT
                // =====================================================

                int timeoutSeconds =
                    settings.AiTimeout;

                if (timeoutSeconds <= 0)
                    timeoutSeconds = 90;

                // =====================================================
                // REQUEST OBJECT
                // =====================================================

                var request =
                    new
                    {
                        model = routerModel,

                        messages =
                            BuildMessages(
                                settings,
                                languageInstruction),

                        temperature =
                            temperature,

                        stream = true
                    };

                string json =
                    JsonConvert.SerializeObject(request);

                Debug.WriteLine(
                    "AI TIMING: REQUEST JSON READY = " +
                    DateTime.Now.ToString("HH:mm:ss.fff"));

                // =====================================================
                // DEBUG
                // =====================================================

                if (settings.DebugLogging)
                {
                    Debug.WriteLine(
                        "========== AI REQUEST ==========");

                    Debug.WriteLine(
                        "Model: " +
                        routerModel);

                    Debug.WriteLine(
                        "Temperature: " +
                        temperature);

                    Debug.WriteLine(
                        "Response Length: " +
                        responseLength);

                    Debug.WriteLine(
                        "Language: " +
                        settings.Language);

                    Debug.WriteLine(
                        "Answer Mode: " +
                        answerMode);

                    Debug.WriteLine(
                        "Voice Cycle Request: " +
                        voiceCycleRequest);

                    Debug.WriteLine(
                        "Question: " +
                        question);

                    Debug.WriteLine(
                        "================================");
                }

                // =====================================================
                // HTTP REQUEST
                // =====================================================

                using (HttpRequestMessage requestMessage =
                       new HttpRequestMessage(
                           HttpMethod.Post,
                           "https://openrouter.ai/api/v1/chat/completions"))
                {
                    requestMessage.Content =
                        new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json");

                    // =================================================
                    // AUTHORIZATION
                    // =================================================

                    requestMessage.Headers.Authorization =
                        new System.Net.Http.Headers
                            .AuthenticationHeaderValue(
                                "Bearer",
                                apiKey);

                    // =================================================
                    // OPENROUTER HEADERS
                    // =================================================

                    requestMessage.Headers.TryAddWithoutValidation(
                        "HTTP-Referer",
                        "http://localhost");

                    requestMessage.Headers.TryAddWithoutValidation(
                        "X-Title",
                        "AI Interview Assistant");

                    Debug.WriteLine(
                        "AI TIMING: BEFORE SEND = " +
                        DateTime.Now.ToString("HH:mm:ss.fff"));

                    // =================================================
                    // SEND
                    // =================================================

                    using (HttpResponseMessage response =
                           await _openRouterClient.SendAsync(
                               requestMessage,
                               HttpCompletionOption.ResponseHeadersRead,
                               cancellationToken)
                           .ConfigureAwait(false))
                    {
                        // =================================================
                        // IMPORTANT:
                        // SendAsync can return immediately around the same
                        // time cancellation is requested.
                        // Do not continue with the response if cancelled.
                        // =================================================

                        if (cancellationToken.IsCancellationRequested)
                        {
                            Debug.WriteLine(
                                "AI REQUEST: Cancelled after SendAsync.");

                            return;
                        }

                        headersTime = DateTime.Now;

                        Debug.WriteLine(
                            "AI TIMING: RESPONSE HEADERS = " +
                            headersTime.Value.ToString("HH:mm:ss.fff"));

                        Debug.WriteLine(
                            "AI TIMING: HEADERS DELAY = " +
                            (headersTime.Value - requestStart)
                                .TotalSeconds.ToString("F2") +
                            " sec");

                        Debug.WriteLine(
                            "AI DEBUG 2: SendAsync completed | Status = " +
                            (int)response.StatusCode);

                        // =================================================
                        // 401
                        // =================================================

                        if ((int)response.StatusCode == 401)
                        {
                            string error =
                                await response.Content
                                    .ReadAsStringAsync()
                                    .ConfigureAwait(false);

                            if (cancellationToken.IsCancellationRequested)
                                return;

                            Debug.WriteLine(
                                "AI 401 ERROR: " +
                                error);

                            await FinalizeAIMessageOnUI(
                                thinkingBubble,
                                "OpenRouter authentication failed:\n\n" +
                                error);

                            return;
                        }

                        // =================================================
                        // 429
                        // =================================================

                        if ((int)response.StatusCode == 429)
                        {
                            string error =
                                await response.Content
                                    .ReadAsStringAsync()
                                    .ConfigureAwait(false);

                            if (cancellationToken.IsCancellationRequested)
                                return;

                            await FinalizeAIMessageOnUI(
                                thinkingBubble,
                                "AI rate limit reached.\n\n" +
                                error);

                            return;
                        }

                        // =================================================
                        // 400
                        // =================================================

                        if ((int)response.StatusCode == 400)
                        {
                            string error =
                                await response.Content
                                    .ReadAsStringAsync()
                                    .ConfigureAwait(false);

                            if (cancellationToken.IsCancellationRequested)
                                return;

                            await FinalizeAIMessageOnUI(
                                thinkingBubble,
                                "AI request error:\n\n" +
                                error);

                            return;
                        }

                        // =================================================
                        // OTHER HTTP ERROR
                        // =================================================

                        if (!response.IsSuccessStatusCode)
                        {
                            string error =
                                await response.Content
                                    .ReadAsStringAsync()
                                    .ConfigureAwait(false);

                            if (cancellationToken.IsCancellationRequested)
                                return;

                            await FinalizeAIMessageOnUI(
                                thinkingBubble,
                                "AI Error (" +
                                (int)response.StatusCode +
                                "):\n\n" +
                                error);

                            return;
                        }

                        // =================================================
                        // STREAM
                        // =================================================

                        using (System.IO.Stream stream =
                               await response.Content
                                   .ReadAsStreamAsync()
                                   .ConfigureAwait(false))

                        using (System.IO.StreamReader reader =
                               new System.IO.StreamReader(
                                   stream,
                                   Encoding.UTF8))
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                Debug.WriteLine(
                                    "AI STREAM: Cancelled before stream processing.");

                                return;
                            }

                            streamTime = DateTime.Now;

                            Debug.WriteLine(
                                "AI TIMING: STREAM OPENED = " +
                                streamTime.Value.ToString("HH:mm:ss.fff"));

                            Debug.WriteLine(
                                "AI TIMING: STREAM OPEN DELAY = " +
                                (streamTime.Value - requestStart)
                                    .TotalSeconds.ToString("F2") +
                                " sec");

                            string fullAnswer = "";

                            // =================================================
                            // READ SSE
                            // =================================================

                            while (true)
                            {
                                // =================================================
                                // CANCELLATION CHECK
                                // =================================================

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: Cancellation requested.");

                                    // Voice-cycle cancellation is silent.
                                    // Manual Stop keeps existing behavior.
                                    if (!voiceCycleRequest)
                                    {
                                        await UpdateAIMessageOnUI(
                                            thinkingBubble,
                                            "Generation stopped.");
                                    }

                                    return;
                                }

                                string line;

                                try
                                {
                                    line =
                                        await ReadLineWithCancellationAsync(
                                            reader,
                                            cancellationToken)
                                            .ConfigureAwait(false);
                                }
                                catch (OperationCanceledException)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: Read cancelled.");

                                    // Voice-cycle cancellation is silent.
                                    // Manual Stop keeps existing behavior.
                                    if (!voiceCycleRequest)
                                    {
                                        await UpdateAIMessageOnUI(
                                            thinkingBubble,
                                            "Generation stopped.");
                                    }

                                    return;
                                }
                                catch (System.IO.IOException ioEx)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM IO ERROR: " +
                                        ioEx.ToString());

                                    // IMPORTANT:
                                    // IO exception caused by cancellation
                                    // must not be treated as a real error.
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        Debug.WriteLine(
                                            "AI STREAM: IO error occurred after cancellation.");

                                        return;
                                    }

                                    if (!string.IsNullOrWhiteSpace(
                                        fullAnswer))
                                    {
                                        break;
                                    }

                                    await FinalizeAIMessageOnUI(
                                        thinkingBubble,
                                        "AI connection was interrupted.");

                                    return;
                                }
                                catch (System.Net.WebException webEx)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM WEB ERROR: " +
                                        webEx.ToString());

                                    // IMPORTANT:
                                    // Web exception caused by cancellation
                                    // must not be treated as a real error.
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        Debug.WriteLine(
                                            "AI STREAM: Web error occurred after cancellation.");

                                        return;
                                    }

                                    if (!string.IsNullOrWhiteSpace(
                                        fullAnswer))
                                    {
                                        break;
                                    }

                                    await FinalizeAIMessageOnUI(
                                        thinkingBubble,
                                        "AI connection was interrupted.");

                                    return;
                                }

                                // =================================================
                                // IMPORTANT:
                                // Cancellation may happen immediately after
                                // ReadLine returns.
                                // Never process that line.
                                // =================================================

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: Line ignored after cancellation.");

                                    return;
                                }

                                // =================================================
                                // END STREAM
                                // =================================================

                                if (line == null)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: End of stream.");

                                    break;
                                }

                                // =================================================
                                // IGNORE EMPTY
                                // =================================================

                                if (string.IsNullOrWhiteSpace(line))
                                    continue;

                                // =================================================
                                // ONLY SSE DATA
                                // =================================================

                                if (!line.StartsWith(
                                    "data:",
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                string data =
                                    line.Substring(5).Trim();

                                // =================================================
                                // DONE
                                // =================================================

                                if (data == "[DONE]")
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: [DONE]");

                                    break;
                                }

                                // =================================================
                                // JSON
                                // =================================================

                                try
                                {
                                    dynamic result =
                                        JsonConvert
                                            .DeserializeObject(data);

                                    string token = "";

                                    if (result != null &&
                                        result.choices != null &&
                                        result.choices.Count > 0)
                                    {
                                        dynamic choice =
                                            result.choices[0];

                                        // -----------------------------------------
                                        // DELTA CONTENT
                                        // -----------------------------------------

                                        try
                                        {
                                            if (choice.delta != null &&
                                                choice.delta.content != null)
                                            {
                                                token =
                                                    choice.delta
                                                        .content
                                                        .ToString();
                                            }
                                        }
                                        catch
                                        {
                                        }

                                        // -----------------------------------------
                                        // MESSAGE CONTENT
                                        // -----------------------------------------

                                        if (string.IsNullOrEmpty(token))
                                        {
                                            try
                                            {
                                                if (choice.message != null &&
                                                    choice.message.content != null)
                                                {
                                                    token =
                                                        choice.message
                                                            .content
                                                            .ToString();
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    }

                                    // =================================================
                                    // TOKEN
                                    // =================================================

                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        // ---------------------------------------------
                                        // CRITICAL CANCELLATION CHECK
                                        // ---------------------------------------------

                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            Debug.WriteLine(
                                                "AI STREAM: Token ignored after cancellation.");

                                            return;
                                        }

                                        // ---------------------------------------------
                                        // FIRST TOKEN TIMING
                                        // ---------------------------------------------

                                        if (!firstTokenReceived)
                                        {
                                            firstTokenReceived = true;

                                            DateTime firstTokenTime =
                                                DateTime.Now;

                                            Debug.WriteLine(
                                                "AI TIMING: FIRST TOKEN = " +
                                                firstTokenTime
                                                    .ToString("HH:mm:ss.fff"));

                                            Debug.WriteLine(
                                                "AI TIMING: FIRST TOKEN DELAY = " +
                                                (firstTokenTime - requestStart)
                                                    .TotalSeconds
                                                    .ToString("F2") +
                                                " sec");

                                            if (headersTime.HasValue)
                                            {
                                                Debug.WriteLine(
                                                    "AI TIMING: SERVER/STREAM WAIT = " +
                                                    (firstTokenTime -
                                                     headersTime.Value)
                                                        .TotalSeconds
                                                        .ToString("F2") +
                                                    " sec");
                                            }
                                        }

                                        // ---------------------------------------------
                                        // APPEND TOKEN
                                        // ---------------------------------------------

                                        fullAnswer +=
                                            token;

                                        latestAiText =
                                            fullAnswer;

                                        // ---------------------------------------------
                                        // UI UPDATE
                                        // ---------------------------------------------

                                        string targetText =
                                            fullAnswer;

                                        Dispatcher.BeginInvoke(
                                            new Action(() =>
                                            {
                                                // IMPORTANT:
                                                // This callback may have been queued
                                                // before Voice ON cancellation.
                                                //
                                                // Never display an old token after
                                                // the voice-cycle generation is cancelled.
                                                if (cancellationToken.IsCancellationRequested)
                                                {
                                                    Debug.WriteLine(
                                                        "AI STREAM: Queued UI update ignored after cancellation.");

                                                    return;
                                                }

                                                aiTargetText =
                                                    targetText;
                                            }));
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    Debug.WriteLine(
                                        "AI JSON ERROR: " +
                                        jsonEx.Message);
                                }
                                catch (Exception chunkEx)
                                {
                                    Debug.WriteLine(
                                        "AI CHUNK ERROR: " +
                                        chunkEx.Message);
                                }
                            }

                            // =====================================================
                            // CRITICAL FINAL CANCELLATION CHECK
                            //
                            // Voice ON may cancel the request after [DONE] or
                            // after the final token but before this section.
                            //
                            // Do NOT commit the answer in that case.
                            // =====================================================

                            if (cancellationToken.IsCancellationRequested)
                            {
                                Debug.WriteLine(
                                    "AI STREAM: Cancellation requested before final response commit.");

                                return;
                            }

                            // =====================================================
                            // FINAL ANSWER
                            // =====================================================

                            if (!string.IsNullOrWhiteSpace(
                                fullAnswer))
                            {
                                // -------------------------------------------------
                                // DOUBLE CHECK before committing anything.
                                // -------------------------------------------------

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: Final answer commit cancelled.");

                                    return;
                                }

                                string finalAnswer =
                                    fullAnswer.Trim();

                                latestAiText =
                                    finalAnswer;

                                // =================================================
                                // FINAL UI UPDATE
                                // =================================================

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: Cancelled before final UI update.");

                                    return;
                                }

                                await Dispatcher.InvokeAsync(
                                    () =>
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            Debug.WriteLine(
                                                "AI STREAM: Final UI callback ignored after cancellation.");

                                            return;
                                        }

                                        StopAITypingAnimation(
                                            thinkingBubble,
                                            finalAnswer);
                                    });

                                // =================================================
                                // FINAL CANCELLATION CHECK
                                // =================================================

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: Cancelled before history commit.");

                                    return;
                                }

                                // =================================================
                                // DEBUG - AI FINAL RESPONSE
                                // =================================================

                                Debug.WriteLine(
                                    "=================================================");

                                Debug.WriteLine(
                                    "AI RESPONSE COMPLETED");

                                Debug.WriteLine(
                                    "QUESTION:");

                                Debug.WriteLine(
                                    question);

                                Debug.WriteLine(
                                    "");

                                Debug.WriteLine(
                                    "ANSWER:");

                                Debug.WriteLine(
                                    finalAnswer);

                                Debug.WriteLine(
                                    "=================================================");

                                // =============================================
                                // ADD ASSISTANT RESPONSE TO HISTORY
                                // =============================================

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: Cancelled before assistant history commit.");

                                    return;
                                }

                                conversationHistory.Add(
                                    new
                                    {
                                        role = "assistant",
                                        content = finalAnswer
                                    });

                                // Voice-cycle user history entry is now
                                // permanently committed.
                                voiceHistoryCommitted = true;

                                Debug.WriteLine(
                                    "AI TIMING: TOTAL RESPONSE TIME = " +
                                    (DateTime.Now - requestStart)
                                        .TotalSeconds
                                        .ToString("F2") +
                                    " sec");

                                Debug.WriteLine(
                                    "AI STREAM: SUCCESS");

                                Debug.WriteLine(
                                    "=================================================");

                                return;
                            }

                            // =================================================
                            // NO ANSWER
                            // =================================================

                            if (cancellationToken.IsCancellationRequested)
                            {
                                Debug.WriteLine(
                                    "AI STREAM: Cancelled with no answer.");

                                // Do not show "Generation stopped."
                                // for voice-cycle cancellation.
                                if (!voiceCycleRequest)
                                {
                                    await UpdateAIMessageOnUI(
                                        thinkingBubble,
                                        "Generation stopped.");
                                }

                                return;
                            }

                            await FinalizeAIMessageOnUI(
                                thinkingBubble,
                                "AI connection was interrupted.");

                            return;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine(
                    "AI REQUEST: OperationCanceledException");

                if (cancellationToken.IsCancellationRequested)
                {
                    // Voice cycle cancellation is intentionally silent.
                    // The retained question will be used by the next cycle.
                    if (!voiceCycleRequest)
                    {
                        await UpdateAIMessageOnUI(
                            thinkingBubble,
                            "Generation stopped.");
                    }
                }
                else
                {
                    await FinalizeAIMessageOnUI(
                        thinkingBubble,
                        "AI connection was interrupted.");
                }
            }
            catch (Exception ex)
            {
                // =====================================================
                // IMPORTANT:
                // A cancellation can surface as another exception type
                // from HttpClient/stream disposal.
                // Never show an error for an already-cancelled request.
                // =====================================================

                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine(
                        "AI FINAL ERROR IGNORED BECAUSE REQUEST WAS CANCELLED.");

                    Debug.WriteLine(
                        ex.ToString());

                    return;
                }

                Debug.WriteLine(
                    "AI FINAL ERROR: " +
                    ex.ToString());

                string errorMessage =
                    "AI request failed:\n\n" +
                    ex.Message;

                if (ex.InnerException != null)
                {
                    errorMessage +=
                        "\n\nInner Exception:\n" +
                        ex.InnerException.Message;
                }

                await UpdateAIMessageOnUI(
                    thinkingBubble,
                    errorMessage);
            }
            finally
            {
                // =====================================================
                // VOICE CYCLE HISTORY CLEANUP
                // =====================================================

                if (voiceCycleRequest &&
                    !voiceHistoryCommitted &&
                    voiceUserHistoryEntry != null)
                {
                    conversationHistory.Remove(
                        voiceUserHistoryEntry);

                    Debug.WriteLine(
                        "VOICE CYCLE: Cancelled/failed AI history entry removed.");
                }

                // =====================================================
                // RELEASE REQUEST LOCK
                // =====================================================

                System.Threading.Interlocked.Exchange(
                    ref _aiRequestInProgress,
                    0);

                // =====================================================
                // RETURN FOCUS TO QUESTION TEXTBOX
                // =====================================================

                await Dispatcher.InvokeAsync(
                    () =>
                    {
                        if (QuestionTextBox == null)
                            return;

                        QuestionTextBox.IsEnabled =
                            true;

                        QuestionTextBox.Focus();

                        QuestionTextBox.CaretIndex =
                            QuestionTextBox.Text?.Length ?? 0;
                    });

                Debug.WriteLine(
                    "AI TIMING: REQUEST END = " +
                    DateTime.Now.ToString("HH:mm:ss.fff"));
            }
        }

        private async Task<string> ReadLineWithCancellationAsync(
            StreamReader reader,
            CancellationToken cancellationToken)
        {
            Task<string> readTask = reader.ReadLineAsync();

            if (!cancellationToken.CanBeCanceled)
            {
                return await readTask.ConfigureAwait(false);
            }

            Task cancellationTask = Task.Delay(
                Timeout.Infinite,
                cancellationToken);

            Task completedTask = await Task.WhenAny(
                readTask,
                cancellationTask).ConfigureAwait(false);

            if (completedTask != readTask)
            {
                // Make sure any exception from the underlying read
                // is observed after the stream gets disposed.
                _ = readTask.ContinueWith(
                    t =>
                    {
                        var ignored = t.Exception;
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted |
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                throw new OperationCanceledException(
                    cancellationToken);
            }

            return await readTask.ConfigureAwait(false);
        }

        // =========================================================
        // CLEAR CONVERSATION
        // =========================================================

        private void ClearConversation()
        {
            if (aiTypingTimer != null)
                aiTypingTimer.Stop();

            aiTypingTimer = null;

            aiTypingBubble = null;

            aiTargetText = "";

            aiDisplayedLength = 0;

            latestAiText = "";

            conversationHistory.Clear();

            AppSettings settings =
                SettingsService.Load()
                ?? new AppSettings();

            string languageInstruction =
                "Answer in natural professional English.";

            conversationHistory.Add(
                new
                {
                    role = "system",

                    content =
                        BuildSystemPrompt(
                            settings,
                            languageInstruction)
                });

            if (ChatPanel != null)
            {
                ChatPanel.Children.Clear();
            }

            if (QuestionTextBox != null)
            {
                QuestionTextBox.Clear();

                QuestionTextBox.IsEnabled =
                    true;

                QuestionTextBox.Focus();
            }

            if (SendButton != null)
            {
                SendButton.IsEnabled =
                    true;

                SendButton.Visibility =
                    Visibility.Visible;
            }

            if (StopButton != null)
            {
                StopButton.Visibility =
                    Visibility.Collapsed;
            }

            isGenerating = false;
        }

        // =========================================================
        // SEND QUESTION
        // =========================================================

        private async Task SendQuestion(
            string question = null,
            Border thinkingBubble = null)
        {
            try
            {
                // =====================================================
                // GET QUESTION
                // =====================================================

                bool questionFromTextBox =
                    string.IsNullOrWhiteSpace(question);

                if (questionFromTextBox)
                {
                    if (QuestionTextBox == null)
                        return;

                    question =
                        QuestionTextBox.Text?.Trim();
                }

                // =====================================================
                // VALIDATE
                // =====================================================

                if (string.IsNullOrWhiteSpace(question))
                    return;

                question = question.Trim();

                // =====================================================
                // SET GENERATING STATE
                // =====================================================

                isGenerating = true;

                latestAiText = "";

                // =====================================================
                // LOAD LATEST SETTINGS
                // =====================================================

                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();

                currentSettings =
                    settings;

                // =====================================================
                // ANSWER MODE
                // =====================================================

                string answerMode =
                        settings.AnswerMode;

                if (string.IsNullOrWhiteSpace(answerMode))
                {
                    answerMode = "Short";
                }

                // =====================================================
                // DEBUG
                // =====================================================

                Debug.WriteLine(
                    "=================================================");

                Debug.WriteLine(
                    "SEND QUESTION STARTED");

                Debug.WriteLine(
                    "Question: " +
                    question);

                Debug.WriteLine(
                    "Source: " +
                    (questionFromTextBox
                        ? "TEXTBOX"
                        : "VOICE"));

                Debug.WriteLine(
                    "=================================================");

                // =====================================================
                // ADD USER MESSAGE
                //
                // Voice already added the message before calling
                // SendQuestion().
                // =====================================================

                if (questionFromTextBox)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        AddUserMessage(question);
                    });
                }

                // =====================================================
                // CREATE AI THINKING BUBBLE
                // =====================================================

                if (thinkingBubble == null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        thinkingBubble =
                            AddAIMessage("");

                        StartAITypingAnimation(
                            thinkingBubble,
                            "");
                    });
                }
                else
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        StartAITypingAnimation(
                            thinkingBubble,
                            "");
                    });
                }

                // =====================================================
                // CLEAR TEXTBOX
                // =====================================================

                if (questionFromTextBox)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (QuestionTextBox != null)
                        {
                            QuestionTextBox.Clear();
                        }
                    });
                }

                // =====================================================
                // UI STATE
                // =====================================================

                await Dispatcher.InvokeAsync(() =>
                {
                    if (QuestionTextBox != null)
                    {
                        QuestionTextBox.IsEnabled = false;
                    }

                    if (SendButton != null)
                    {
                        SendButton.IsEnabled = false;
                    }

                    if (StopButton != null)
                    {
                        StopButton.Visibility =
                            Visibility.Visible;

                        StopButton.IsEnabled = true;
                    }
                });

                // =====================================================
                // CREATE CANCELLATION TOKEN
                // =====================================================

                if (cancellationTokenSource != null)
                {
                    try
                    {
                        cancellationTokenSource.Dispose();
                    }
                    catch
                    {
                    }
                }

                cancellationTokenSource =
                    new CancellationTokenSource();

                CancellationToken token =
                    cancellationTokenSource.Token;

                // =====================================================
                // SEND TO OPENROUTER
                // =====================================================

                Debug.WriteLine(
                    "SEND QUESTION: Calling AskOpenRouterStreaming...");

                await AskOpenRouterStreaming(
                    question,
                    thinkingBubble,
                    token,
                    answerMode);

                // =====================================================
                // FINAL AI RESPONSE / FINAL ERROR
                //
                // AskOpenRouterStreaming() returns only after the
                // final AI message or final error has been written
                // into the bubble.
                // =====================================================

                if (!token.IsCancellationRequested &&
                    thinkingBubble != null)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        string finalAiText =
                            GetCompletedAIMessageText(
                                thinkingBubble);

                        if (!string.IsNullOrWhiteSpace(finalAiText))
                        {
                            RecordCompletedAIMessage(
                                thinkingBubble,
                                finalAiText);
                        }
                        else
                        {
                            Debug.WriteLine(
                                "INTERVIEW RECORD: Final Message Send AI text not available.");
                        }
                    });
                }

                Debug.WriteLine(
                    "SEND QUESTION: AI RESPONSE COMPLETED");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine(
                    "SEND QUESTION: Cancelled");

                if (thinkingBubble != null)
                {
                    await UpdateAIMessageOnUI(
                        thinkingBubble,
                        "Generation stopped.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "SEND QUESTION ERROR:");

                Debug.WriteLine(
                    ex.ToString());

                if (thinkingBubble != null)
                {
                    await UpdateAIMessageOnUI(
                        thinkingBubble,
                        "Send error:\n\n" +
                        ex.Message);
                }
            }
            finally
            {
                // =====================================================
                // RESTORE UI
                // =====================================================

                await Dispatcher.InvokeAsync(() =>
                {
                    if (QuestionTextBox != null)
                    {
                        QuestionTextBox.IsEnabled = true;
                    }

                    if (SendButton != null)
                    {
                        SendButton.IsEnabled = true;
                    }

                    if (StopButton != null)
                    {
                        StopButton.Visibility =
                            Visibility.Collapsed;

                        StopButton.IsEnabled = false;
                    }

                    isGenerating = false;
                });
            }
        }
    }
}