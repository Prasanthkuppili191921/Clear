using AiInterviewAssistant.Settings.Resume;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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
    string answerMode)
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
                    await UpdateAIMessageOnUI(
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
                    await UpdateAIMessageOnUI(
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
                //
                // Smart ON:
                // DO NOT load/use Response Length.
                //
                // Smart OFF:
                // Existing Response Length behavior remains.
                // =====================================================

                string responseLength = null;


                if (!_smartAnswerEnabled)
                {
                    responseLength =
                        settings.ResponseLength;


                    if (string.IsNullOrWhiteSpace(responseLength))
                        responseLength = "Medium";
                }


                // =====================================================
                // LANGUAGE
                // =====================================================

                string languageInstruction =
                    "Answer in natural professional English.";


                // =====================================================
                // ANSWER MODE / SMART ANSWER
                // =====================================================

                string finalQuestion;


                if (_smartAnswerEnabled)
                {
                    // -------------------------------------------------
                    // SMART ANSWER ON
                    //
                    // IMPORTANT:
                    // Do NOT send Answer Mode.
                    // Do NOT send Response Length.
                    //
                    // SmartAnswerService is added through
                    // BuildMessages().
                    // -------------------------------------------------

                    finalQuestion =
                        languageInstruction +
                        "\n\n" +
                        "Interview question:\n" +
                        question;
                }
                else
                {
                    // -------------------------------------------------
                    // SMART ANSWER OFF
                    //
                    // Existing behavior remains unchanged.
                    // -------------------------------------------------

                    string modeInstruction =
                        BuildAnswerModeInstruction(
                            answerMode,
                            responseLength);


                    finalQuestion =
                        modeInstruction +
                        "\n\n" +
                        languageInstruction +
                        "\n\n" +
                        "Interview question:\n" +
                        question;
                }


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

                conversationHistory.Add(
                    new
                    {
                        role = "user",
                        content = finalQuestion
                    });


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


                            Debug.WriteLine(
                                "AI 401 ERROR: " +
                                error);


                            await UpdateAIMessageOnUI(
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


                            await UpdateAIMessageOnUI(
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


                            await UpdateAIMessageOnUI(
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


                            await UpdateAIMessageOnUI(
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
                                if (cancellationToken
                                    .IsCancellationRequested)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM: Cancellation requested.");


                                    await UpdateAIMessageOnUI(
                                        thinkingBubble,
                                        "Generation stopped.");


                                    return;
                                }


                                string line;


                                try
                                {
                                    line =
                                        await reader
                                            .ReadLineAsync()
                                            .ConfigureAwait(false);
                                }
                                catch (System.IO.IOException ioEx)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM IO ERROR: " +
                                        ioEx.ToString());


                                    if (!string.IsNullOrWhiteSpace(
                                            fullAnswer))
                                    {
                                        break;
                                    }


                                    await UpdateAIMessageOnUI(
                                        thinkingBubble,
                                        "AI connection was interrupted.");


                                    return;
                                }
                                catch (System.Net.WebException webEx)
                                {
                                    Debug.WriteLine(
                                        "AI STREAM WEB ERROR: " +
                                        webEx.ToString());


                                    if (!string.IsNullOrWhiteSpace(
                                            fullAnswer))
                                    {
                                        break;
                                    }


                                    await UpdateAIMessageOnUI(
                                        thinkingBubble,
                                        "AI connection was interrupted.");


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


                                        fullAnswer +=
                                            token;


                                        latestAiText =
                                            fullAnswer;


                                        string targetText =
                                            fullAnswer;


                                        Dispatcher.BeginInvoke(
                                            new Action(() =>
                                            {
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


                            // =================================================
                            // FINAL ANSWER
                            // =================================================

                            if (!string.IsNullOrWhiteSpace(
                                    fullAnswer))
                            {
                                string finalAnswer =
                                    fullAnswer.Trim();


                                latestAiText =
                                    finalAnswer;


                                await Dispatcher.InvokeAsync(
                                    () =>
                                    {
                                        StopAITypingAnimation(
                                            thinkingBubble,
                                            finalAnswer);
                                    });


                                // =============================================
                                // ADD ASSISTANT RESPONSE TO HISTORY
                                // =============================================

                                conversationHistory.Add(
                                    new
                                    {
                                        role = "assistant",
                                        content = finalAnswer
                                    });


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

                            if (cancellationToken
                                .IsCancellationRequested)
                            {
                                await UpdateAIMessageOnUI(
                                    thinkingBubble,
                                    "Generation stopped.");


                                return;
                            }


                            await UpdateAIMessageOnUI(
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
                    await UpdateAIMessageOnUI(
                        thinkingBubble,
                        "Generation stopped.");
                }
                else
                {
                    await UpdateAIMessageOnUI(
                        thinkingBubble,
                        "AI connection was interrupted.");
                }
            }
            catch (Exception ex)
            {
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


            string languageInstruction = "Answer in natural professional English.";
           
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

                string answerMode = null;

                if (!_smartAnswerEnabled)
                {
                    answerMode =
                        settings.AnswerMode;

                    if (string.IsNullOrWhiteSpace(answerMode))
                    {
                        answerMode = "Short";
                    }
                }

                // =====================================================
                // CHATGPT VIEW
                // =====================================================

                if (_chatGPTView)
                {
                    Debug.WriteLine(
                        "SEND QUESTION: ChatGPT View enabled.");

                    Debug.WriteLine(
                        "SEND QUESTION: Sending question to ChatGPT WebView.");

                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (QuestionTextBox != null)
                        {
                            QuestionTextBox.Clear();
                        }
                    });

                    await ChatGPTWebViewHost.SendQuestionAsync(
                        question);

                    Debug.WriteLine(
                        "SEND QUESTION: ChatGPT WebView send completed.");

                    return;
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
