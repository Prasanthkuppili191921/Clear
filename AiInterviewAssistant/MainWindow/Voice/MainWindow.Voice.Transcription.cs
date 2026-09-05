using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // OPENROUTER GEMINI AUDIO STT
        // =========================================================

        private const string
            OpenRouterSpeechToTextEndpoint =
                "https://openrouter.ai/api/v1/chat/completions";


        // =========================================================
        // TRANSCRIBE AUDIO
        // =========================================================

        private async Task<string>
            TranscribeWithOpenRouterAsync(
                byte[] audioBytes)
        {
            if (audioBytes == null ||
                audioBytes.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                // =====================================================
                // SETTINGS
                // =====================================================

                AppSettings settings =
                    SettingsService.Load()
                    ?? new AppSettings();

                string apiKey =
                    settings.OpenRouterApiKey?.Trim();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException(
                        "OpenRouter API key is missing.");
                }


                // =====================================================
                // MODEL
                // =====================================================

                string speechToTextModel =
                    settings.SpeechToTextModel?.Trim();

                if (string.IsNullOrWhiteSpace(
                    speechToTextModel))
                {
                    speechToTextModel =
                        "google/gemini-2.5-flash-lite";
                }


                Debug.WriteLine(
                    "========================================");

                Debug.WriteLine(
                    "GEMINI FLASH-LITE STT");

                Debug.WriteLine(
                    "STT MODEL = " +
                    speechToTextModel);

                Debug.WriteLine(
                    "STT WAV SIZE = " +
                    audioBytes.Length +
                    " bytes");

                Debug.WriteLine(
                    "========================================");


                Stopwatch totalTimer =
                    Stopwatch.StartNew();


                // =====================================================
                // BASE64
                // =====================================================

                Stopwatch base64Timer =
                    Stopwatch.StartNew();

                string base64Audio =
                    Convert.ToBase64String(
                        audioBytes);

                base64Timer.Stop();

                Debug.WriteLine(
                    "STT BASE64 TIME = " +
                    base64Timer.ElapsedMilliseconds +
                    " ms");


                // =====================================================
                // ROOT JSON
                // =====================================================

                JObject requestBody =
                    new JObject();

                requestBody["model"] =
                    speechToTextModel;


                // =====================================================
                // MESSAGE
                // =====================================================

                JObject message =
                    new JObject();

                message["role"] =
                    "user";


                // =====================================================
                // CONTENT
                // =====================================================

                JArray content =
                    new JArray();


                // =====================================================
                // TEXT PART
                // =====================================================

                JObject textPart =
                    new JObject();

                textPart["type"] =
                    "text";

                //textPart["text"] =
                //    "Transcribe this audio exactly. " +
                //    "Return ONLY the spoken words. " +
                //    "Do not summarize. " +
                //    "Do not explain. " +
                //    "Do not add commentary. " +
                //    "Keep the original English wording.";

                textPart["text"] =
                    "Transcribe this audio exactly as spoken. " +
                    "Return ONLY the spoken words. " +
                    "Do not summarize, interpret, paraphrase, or rewrite anything. " +
                    "This is a software engineering interview. " +
                    "Preserve programming and technical terminology exactly as spoken. " +
                    "Do not replace technical terms with similar-sounding names or ordinary English words. " +
                    "For example, preserve terms such as jQuery, JavaScript, HTML, CSS, DOM, div, element, API, " +
                    "ASP.NET, ASP.NET Core, C#, .NET, React, Angular, Entity Framework, and REST API. " +
                    "If a technical term sounds like a person's name or another English word, keep the intended technical term. " +
                    "Keep the original English wording.";

                content.Add(
                    textPart);


                // =====================================================
                // AUDIO PART
                // =====================================================

                JObject audioPart =
                    new JObject();

                audioPart["type"] =
                    "input_audio";


                JObject inputAudio =
                    new JObject();

                inputAudio["data"] =
                    base64Audio;

                inputAudio["format"] =
                    "wav";


                audioPart["input_audio"] =
                    inputAudio;


                content.Add(
                    audioPart);


                // =====================================================
                // COMPLETE MESSAGE
                // =====================================================

                message["content"] =
                    content;


                JArray messages =
                    new JArray();

                messages.Add(
                    message);


                requestBody["messages"] =
                    messages;


                // =====================================================
                // TEMPERATURE
                // =====================================================

                requestBody["temperature"] =
                    0;


                // =====================================================
                // FINAL JSON
                // =====================================================

                string json =
                    requestBody.ToString(
                        Formatting.None);


                Debug.WriteLine(
                    "STT JSON SIZE = " +
                    json.Length);


                // =====================================================
                // HTTP REQUEST
                // =====================================================

                using (HttpRequestMessage request =
                       new HttpRequestMessage(
                           HttpMethod.Post,
                           OpenRouterSpeechToTextEndpoint))
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers
                            .AuthenticationHeaderValue(
                                "Bearer",
                                apiKey);


                    request.Headers.TryAddWithoutValidation(
                        "HTTP-Referer",
                        "https://openrouter.ai");


                    request.Headers.TryAddWithoutValidation(
                        "X-OpenRouter-Title",
                        "AI Interview Assistant");


                    request.Content =
                        new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json");


                    // =================================================
                    // SEND
                    // =================================================

                    Stopwatch httpTimer =
                        Stopwatch.StartNew();


                    using (HttpResponseMessage response =
                           await voiceHttpClient.SendAsync(
                               request,
                               HttpCompletionOption.ResponseHeadersRead))
                    {
                        httpTimer.Stop();


                        Debug.WriteLine(
                            "STT HTTP TIME = " +
                            httpTimer.ElapsedMilliseconds +
                            " ms");


                        string responseText =
                            await response.Content
                                .ReadAsStringAsync();


                        // =============================================
                        // STATUS
                        // =============================================

                        Debug.WriteLine(
                            "OPENROUTER STT STATUS = " +
                            (int)response.StatusCode);


                        // =============================================
                        // ERROR
                        // =============================================

                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine(
                                "OPENROUTER STT ERROR:");

                            Debug.WriteLine(
                                responseText);

                            throw new InvalidOperationException(
                                "Gemini speech-to-text failed.\n\n" +
                                "HTTP " +
                                (int)response.StatusCode +
                                "\n\n" +
                                ExtractOpenRouterError(
                                    responseText));
                        }


                        // =============================================
                        // RAW RESPONSE
                        // =============================================

                        Debug.WriteLine(
                            "GEMINI RAW RESPONSE:");

                        Debug.WriteLine(
                            responseText);


                        // =============================================
                        // PARSE
                        // =============================================

                        JObject root =
                            JObject.Parse(
                                responseText);


                        string finalText =
                            root["choices"]?[0]?["message"]?["content"]
                                ?.ToString()
                                ?.Trim()
                            ?? string.Empty;


                        totalTimer.Stop();


                        // =============================================
                        // RESULT
                        // =============================================

                        Debug.WriteLine(
                            "TOTAL STT TIME = " +
                            totalTimer.ElapsedMilliseconds +
                            " ms");


                        Debug.WriteLine(
                            "GEMINI STT FINAL TEXT = [" +
                            finalText +
                            "]");


                        Debug.WriteLine(
                            "========================================");


                        return finalText;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "OPENROUTER GEMINI STT ERROR:");

                Debug.WriteLine(
                    ex.ToString());

                throw;
            }
        }


        // =========================================================
        // ERROR EXTRACTION
        // =========================================================

        private string ExtractOpenRouterError(
            string responseText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(
                    responseText))
                {
                    return "Unknown OpenRouter error.";
                }


                JObject root =
                    JObject.Parse(
                        responseText);


                string message =
                    root["error"]?["message"]
                        ?.ToString();


                if (!string.IsNullOrWhiteSpace(
                    message))
                {
                    return message;
                }
            }
            catch
            {
            }


            return responseText;
        }
    }
}