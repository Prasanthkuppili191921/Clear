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
        private const string OpenRouterSpeechToTextEndpoint =
            "https://openrouter.ai/api/v1/chat/completions";

        private async Task<string> TranscribeWithOpenRouterAsync(byte[] audioBytes)
        {
            if (audioBytes == null || audioBytes.Length == 0)
                return string.Empty;

            try
            {
                // =====================================================
                // SILERO VAD
                // IMPORTANT: ChatGPT WebView is intentionally excluded.
                // Its voice path must remain completely untouched.
                // =====================================================
                if (!_chatGPTView)
                {
                    Stopwatch vadTimer = Stopwatch.StartNew();

                    byte[] vadAudio =
                        SileroVadService.RemoveSilence(audioBytes);

                    vadTimer.Stop();

                    if (vadAudio == null || vadAudio.Length <= 44)
                    {
                        Debug.WriteLine("SILERO VAD: no speech audio remained.");
                        return string.Empty;
                    }

                    Debug.WriteLine(
                        "SILERO VAD TIME = " +
                        vadTimer.ElapsedMilliseconds + " ms");

                    Debug.WriteLine(
                        "STT AUDIO BEFORE VAD = " +
                        audioBytes.Length + " bytes");

                    Debug.WriteLine(
                        "STT AUDIO AFTER VAD = " +
                        vadAudio.Length + " bytes");

                    audioBytes = vadAudio;
                }

                AppSettings settings =
                    SettingsService.Load() ?? new AppSettings();

                string apiKey =
                    settings.OpenRouterApiKey?.Trim();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new InvalidOperationException(
                        "OpenRouter API key is missing.");
                }

                string speechToTextModel =
                    settings.SpeechToTextModel?.Trim();

                if (string.IsNullOrWhiteSpace(speechToTextModel))
                {
                    speechToTextModel =
                        "google/gemini-2.5-flash-lite";
                }

                Debug.WriteLine("========================================");
                Debug.WriteLine("GEMINI FLASH-LITE STT");
                Debug.WriteLine("STT MODEL = " + speechToTextModel);
                Debug.WriteLine("STT WAV SIZE = " + audioBytes.Length + " bytes");
                Debug.WriteLine("========================================");

                Stopwatch totalTimer = Stopwatch.StartNew();

                string base64Audio =
                    Convert.ToBase64String(audioBytes);

                JObject requestBody = new JObject();
                requestBody["model"] = speechToTextModel;

                JObject message = new JObject();
                message["role"] = "user";

                JArray content = new JArray();

                JObject textPart = new JObject();
                textPart["type"] = "text";
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

                content.Add(textPart);

                JObject audioPart = new JObject();
                audioPart["type"] = "input_audio";

                JObject inputAudio = new JObject();
                inputAudio["data"] = base64Audio;
                inputAudio["format"] = "wav";

                audioPart["input_audio"] = inputAudio;
                content.Add(audioPart);

                message["content"] = content;

                JArray messages = new JArray();
                messages.Add(message);
                requestBody["messages"] = messages;
                requestBody["temperature"] = 0;

                string json = requestBody.ToString(Formatting.None);

                using (HttpRequestMessage request =
                       new HttpRequestMessage(
                           HttpMethod.Post,
                           OpenRouterSpeechToTextEndpoint))
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(
                            "Bearer",
                            apiKey);

                    request.Headers.TryAddWithoutValidation(
                        "HTTP-Referer",
                        "https://openrouter.ai");

                    request.Headers.TryAddWithoutValidation(
                        "X-OpenRouter-Title",
                        "AI Interview Assistant");

                    request.Content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                    Stopwatch httpTimer = Stopwatch.StartNew();

                    using (HttpResponseMessage response =
                           await voiceHttpClient.SendAsync(
                               request,
                               HttpCompletionOption.ResponseHeadersRead))
                    {
                        httpTimer.Stop();

                        Debug.WriteLine(
                            "STT HTTP TIME = " +
                            httpTimer.ElapsedMilliseconds + " ms");

                        string responseText =
                            await response.Content.ReadAsStringAsync();

                        Debug.WriteLine(
                            "OPENROUTER STT STATUS = " +
                            (int)response.StatusCode);

                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine("OPENROUTER STT ERROR:");
                            Debug.WriteLine(responseText);

                            throw new InvalidOperationException(
                                "Gemini speech-to-text failed.\n\n" +
                                "HTTP " +
                                (int)response.StatusCode +
                                "\n\n" +
                                ExtractOpenRouterError(responseText));
                        }

                        JObject root = JObject.Parse(responseText);

                        string finalText =
                            root["choices"]?[0]?["message"]?["content"]
                                ?.ToString()
                                ?.Trim()
                            ?? string.Empty;

                        totalTimer.Stop();

                        Debug.WriteLine(
                            "TOTAL STT TIME = " +
                            totalTimer.ElapsedMilliseconds + " ms");

                        Debug.WriteLine(
                            "GEMINI STT FINAL TEXT = [" +
                            finalText + "]");

                        Debug.WriteLine("========================================");

                        return finalText;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OPENROUTER GEMINI STT ERROR:");
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        private string ExtractOpenRouterError(string responseText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(responseText))
                    return "Unknown OpenRouter error.";

                JObject root = JObject.Parse(responseText);

                string message =
                    root["error"]?["message"]?.ToString();

                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            catch
            {
            }

            return responseText;
        }
    }
}
