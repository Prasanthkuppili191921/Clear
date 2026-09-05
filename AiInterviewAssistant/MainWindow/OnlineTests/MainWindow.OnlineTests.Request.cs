// MainWindow.OnlineTests.Request.cs

using AiInterviewAssistant.Security;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        // =========================================================
        // VISION API
        // =========================================================

        private async Task<VisionResult>
            AskVisionModelAsync(
                string base64Image)
        {
            AppSettings settings =
                SettingsService.Load()
                ?? new AppSettings();

            if (!settings.VisionEnabled)
            {
                throw new InvalidOperationException(
                    "Vision AI is disabled in Settings.");
            }

            // =====================================================
            // API KEY
            // =====================================================

            string apiKey =
                settings.OpenRouterApiKey;

            //if (string.IsNullOrWhiteSpace(apiKey))
            //{
            //    apiKey =
            //        ConfigurationManager
            //            .AppSettings[
            //                "OpenRouterKey"];
            //}

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = OpenRouterKeyProtection.GetKey();
               
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "OpenRouter API key is not configured.");
            }

            // =====================================================
            // MODEL
            // =====================================================

            string model =
                settings.OnlineTestModel;

            if (string.IsNullOrWhiteSpace(model))
            {
                model =
                    ConfigurationManager
                        .AppSettings[
                            "OpenRouterVisionModel"];
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                throw new InvalidOperationException(
                    "Vision model is not configured.");
            }

            // =====================================================
            // QUESTION DETECTION
            // =====================================================

            string detectionInstruction;

            if (string.Equals(
                    settings.QuestionDetection,
                    "Manual",
                    StringComparison.OrdinalIgnoreCase))
            {
                detectionInstruction =
                    "Question Detection Mode: Manual.\n\n" +
                    "Process only the clearly visible question " +
                    "and answer options from the webpage.\n" +
                    "Do not infer a question from unrelated " +
                    "page content.\n\n";
            }
            else
            {
                detectionInstruction =
                    "Question Detection Mode: Auto.\n\n" +
                    "Automatically identify the actual question " +
                    "visible on the webpage and its answer options.\n" +
                    "Ignore unrelated page content.\n\n";
            }

            // =====================================================
            // PROMPT
            // =====================================================

            string prompt =
                "You are an online test question answering assistant.\n\n" +

                detectionInstruction +

                "Read the screenshot carefully.\n\n" +

                "Identify the actual question visible on the webpage " +
                "and all answer options.\n\n" +

                "Ignore browser tabs, address bar, menus, ads, " +
                "sidebars and unrelated content.\n\n" +

                "If there is code, image, diagram or table required " +
                "to answer the question, understand it.\n\n" +

                "Determine the correct answer.\n\n" +

                "IMPORTANT OUTPUT FORMAT:\n\n" +

                "QUESTION:\n" +
                "<complete question and options>\n\n" +

                "ANSWER:\n" +
                "Correct answer: <option>\n" +
                "Explanation: <short explanation>\n\n" +

                "Do not use JSON.\n" +
                "Do not use markdown code blocks.\n" +
                "Do not describe the screenshot.\n" +

                "Do not return safety messages, moderation messages, " +
                "system messages, or phrases such as 'User Safety: safe'.\n\n" +

                "If you cannot identify the question, return:\n\n" +

                "QUESTION:\n\n" +

                "ANSWER:\n" +
                "Unable to identify the question.\n\n" +

                "Never put anything other than the actual webpage question " +
                "and its options inside QUESTION.";

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
            // MAX TOKENS
            // =====================================================

            int maxTokens =
                GetVisionMaxResponseTokens(
                    settings.ResponseLength);

            DebugLog(
                "Vision Temperature: " +
                temperature.ToString(
                    System.Globalization.CultureInfo.InvariantCulture));

            DebugLog(
                "Vision Response Length: " +
                (settings.ResponseLength ?? "Medium"));

            DebugLog(
                "Vision Max Tokens: " +
                maxTokens);

            // =====================================================
            // REQUEST BODY
            // =====================================================

            var requestBody =
                new
                {
                    model = model,

                    messages =
                        new object[]
                        {
                            new
                            {
                                role = "user",

                                content =
                                    new object[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = prompt
                                        },

                                        new
                                        {
                                            type = "image_url",

                                            image_url =
                                                new
                                                {
                                                    url =
                                                        "data:image/jpeg;base64," +
                                                        base64Image
                                                }
                                        }
                                    }
                            }
                        },

                    temperature = temperature,

                    max_tokens = maxTokens
                };

            string json =
                JsonConvert.SerializeObject(
                    requestBody);

            // =====================================================
            // HTTP CLIENT
            // =====================================================

            using (HttpClient client =
                   new HttpClient())
            {
                int timeoutSeconds =
                    settings.AiTimeout;

                if (timeoutSeconds < 60)
                {
                    timeoutSeconds = 60;
                }

                client.Timeout =
                    TimeSpan.FromSeconds(
                        timeoutSeconds);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        apiKey);

                client.DefaultRequestHeaders
                    .TryAddWithoutValidation(
                        "HTTP-Referer",
                        "http://localhost");

                client.DefaultRequestHeaders
                    .TryAddWithoutValidation(
                        "X-Title",
                        "AI Interview Assistant");

                using (StringContent content =
                       new StringContent(
                           json,
                           Encoding.UTF8,
                           "application/json"))
                {
                    HttpResponseMessage response =
                        await client.PostAsync(
                            "https://openrouter.ai/api/v1/chat/completions",
                            content);

                    string responseText =
                        await response.Content
                            .ReadAsStringAsync();

                    if ((int)response.StatusCode == 429)
                    {
                        throw new InvalidOperationException(
                            "Vision AI rate limit reached.\n\n" +
                            "Please wait and try again later.");
                    }

                    if ((int)response.StatusCode == 401)
                    {
                        throw new InvalidOperationException(
                            "OpenRouter API key is invalid or expired.");
                    }

                    if ((int)response.StatusCode == 400)
                    {
                        throw new InvalidOperationException(
                            "Vision AI request error:\n\n" +
                            responseText);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            "OpenRouter Vision API error:\n\n" +
                            responseText);
                    }

                    dynamic result =
                        JsonConvert.DeserializeObject(
                            responseText);

                    string aiContent =
                        result?
                            .choices?[0]?
                            .message?
                            .content?
                            .ToString();

                    if (string.IsNullOrWhiteSpace(
                            aiContent))
                    {
                        throw new InvalidOperationException(
                            "Vision model returned an empty response.");
                    }

                    return ParseVisionResponse(
                        aiContent);
                }
            }
        }

        // =========================================================
        // VISION RESPONSE TOKENS
        // =========================================================

        private int GetVisionMaxResponseTokens(
            string responseLength)
        {
            if (string.IsNullOrWhiteSpace(
                    responseLength))
            {
                return 400;
            }

            switch (
                responseLength
                    .Trim()
                    .ToLowerInvariant())
            {
                case "short":
                    return 200;

                case "long":
                    return 700;

                case "medium":
                default:
                    return 400;
            }
        }


        // =========================================================
        // PARSE VISION RESPONSE
        // =========================================================

        private VisionResult ParseVisionResponse(
            string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return new VisionResult
                {
                    Question = "",
                    Answer = ""
                };
            }

            string cleaned =
                response.Trim();

            int questionIndex =
                cleaned.IndexOf(
                    "QUESTION:",
                    StringComparison.OrdinalIgnoreCase);

            int answerIndex =
                cleaned.IndexOf(
                    "ANSWER:",
                    StringComparison.OrdinalIgnoreCase);

            string question = "";
            string answer = "";

            if (questionIndex >= 0)
            {
                int questionStart =
                    questionIndex +
                    "QUESTION:".Length;

                if (answerIndex > questionStart)
                {
                    question =
                        cleaned.Substring(
                            questionStart,
                            answerIndex -
                            questionStart);
                }
                else
                {
                    question =
                        cleaned.Substring(
                            questionStart);
                }

                question =
                    question.Trim();
            }

            if (answerIndex >= 0)
            {
                int answerStart =
                    answerIndex +
                    "ANSWER:".Length;

                answer =
                    cleaned.Substring(
                        answerStart)
                        .Trim();
            }

            if (string.IsNullOrWhiteSpace(answer))
            {
                answer = cleaned;
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                question =
                    "Question detected from screen";
            }

            return new VisionResult
            {
                Question = question,
                Answer = answer
            };
        }
    }
}