using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiInterviewAssistant.ScreenQuestion
{
    public sealed class UniversalScreenQuestionService
    {
        private readonly HttpClient _httpClient;

        public UniversalScreenQuestionService(HttpClient httpClient)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<UniversalScreenQuestionResult>
            AnalyzeScreenAsync(
                string imageBase64,
                string apiKey,
                string model,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageBase64))
            {
                return new UniversalScreenQuestionResult
                {
                    IsQuestion = false,
                    IsSkillRelated = false,
                    Reason = "Screenshot is empty."
                };
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Vision API key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                throw new InvalidOperationException(
                    "Vision model is not configured.");
            }

            string prompt = BuildPrompt();

            var requestBody = new
            {
                model = model,

                messages = new[]
                {
                    new
                    {
                        role = "user",

                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = prompt
                            },

                            new
                            {
                                type = "image_url",

                                image_url = new
                                {
                                    url =
                                        "data:image/jpeg;base64," +
                                        imageBase64
                                }
                            }
                        }
                    }
                },

                temperature = 0.1,

                max_tokens = 1200
            };

            string json =
                JsonConvert.SerializeObject(requestBody);

            using (var request =
                   new HttpRequestMessage(
                       HttpMethod.Post,
                       "https://openrouter.ai/api/v1/chat/completions"))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer",
                        apiKey);

                request.Content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                using (HttpResponseMessage response =
                       await _httpClient.SendAsync(
                           request,
                           cancellationToken))
                {
                    string responseText =
                        await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            "Vision request failed: " +
                            response.StatusCode +
                            "\n\n" +
                            responseText);
                    }

                    return ParseResponse(responseText);
                }
            }
        }

        private UniversalScreenQuestionResult ParseResponse(
            string responseText)
        {
            try
            {
                dynamic root =
                    JsonConvert.DeserializeObject(responseText);

                string content =
                    root?.choices?[0]?.message?.content?.ToString();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return new UniversalScreenQuestionResult
                    {
                        IsQuestion = false,
                        IsSkillRelated = false,
                        Reason =
                            "Vision model returned empty response."
                    };
                }

                content =
                    RemoveMarkdownCodeFence(content);

                UniversalScreenQuestionResult result =
                    JsonConvert.DeserializeObject
                        <UniversalScreenQuestionResult>(content);

                return result
                    ?? new UniversalScreenQuestionResult
                    {
                        IsQuestion = false,
                        IsSkillRelated = false,
                        Reason =
                            "Unable to parse vision response."
                    };
            }
            catch (Exception ex)
            {
                return new UniversalScreenQuestionResult
                {
                    IsQuestion = false,
                    IsSkillRelated = false,
                    Reason =
                        "Unable to parse vision response: " +
                        ex.Message
                };
            }
        }

        private string RemoveMarkdownCodeFence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Trim();

            if (text.StartsWith("```"))
            {
                int firstNewLine =
                    text.IndexOf('\n');

                if (firstNewLine >= 0)
                {
                    text =
                        text.Substring(
                            firstNewLine + 1);
                }

                int lastFence =
                    text.LastIndexOf("```");

                if (lastFence >= 0)
                {
                    text =
                        text.Substring(
                            0,
                            lastFence);
                }
            }

            return text.Trim();
        }

        private string BuildPrompt()
        {
            return @"
                    You are an interview question detection and relevance analyzer.

                    Analyze the screenshot provided in THIS request.

                    Determine whether there is a current interview, technical assessment,
                    coding, SQL, system design, or technical discussion question visible
                    on the screen.

                    IMPORTANT:

                    Each screenshot is a completely independent request.

                    Do NOT remember or reuse a question from a previous screenshot.

                    Do NOT use information from previous requests.

                    ========================================================
                    CURRENT QUESTION DETECTION
                    ========================================================

                    Find the PRIMARY and CURRENT question visible in the screenshot.

                    The question may be displayed in ANY application:

                    - Notepad
                    - Browser
                    - Chrome
                    - Edge
                    - PDF viewer
                    - VS Code
                    - Visual Studio
                    - Teams
                    - Zoom
                    - Interview platforms
                    - Online coding platforms
                    - SQL tools
                    - Any other desktop application

                    Do NOT assume that the question comes from a browser.

                    If multiple questions or text blocks are visible, select the question
                    belonging to the currently active application or current interview
                    content.

                    Prefer the question that is currently being displayed by the active
                    application.

                    ========================================================
                    IGNORE AI ASSISTANT CONTENT
                    ========================================================

                    The screenshot may contain the AI Interview Assistant itself.

                    Ignore all content belonging to the AI Interview Assistant.

                    Do NOT treat the following as the current interview question:

                    - Previous detected questions
                    - Previous user question bubbles
                    - Previous AI answers
                    - Previous AI response bubbles
                    - Reading question messages
                    - Assistant chat history
                    - Assistant status messages
                    - Assistant controls
                    - Any question displayed inside the AI Interview Assistant

                    If an application window and the AI assistant are both visible,
                    prefer the question from the application window.

                    ========================================================
                    CANDIDATE SKILL PROFILE
                    ========================================================

                    - C#
                    - .NET Framework
                    - .NET 8
                    - ASP.NET Core
                    - Web API
                    - REST APIs
                    - Microservices
                    - Entity Framework
                    - SQL Server
                    - AWS
                    - Angular
                    - React
                    - System Design
                    - GenAI
                    - LLM
                    - AI integrations

                    A question does not need to exactly match a skill name.

                    Related concepts such as:

                    - API Gateway
                    - Caching
                    - Authentication
                    - Authorization
                    - Distributed systems
                    - Concurrency
                    - Design patterns
                    - Microservices
                    - Cloud architecture

                    may also be considered skill related.

                    ========================================================
                    TASKS
                    ========================================================

                    1. Find the current interview question visible on the screen.

                    2. Ignore application headers, menus, navigation, timers, buttons,
                       advertisements, profile information and unrelated text.

                    3. If multiple questions are visible, select the primary current
                       question from the active application.

                    4. Include MCQ options when they belong to the question.

                    5. Identify the question type.

                    6. Determine whether the question is related to the candidate skill
                       profile.

                    7. Do not answer the question.

                    8. Only extract and classify the question.

                    ========================================================
                    QUESTION TYPES
                    ========================================================

                    Possible question types:

                    - MCQ
                    - Coding
                    - SQL
                    - .NET
                    - C#
                    - ASP.NET Core
                    - Web API
                    - Microservices
                    - AWS
                    - Angular
                    - React
                    - System Design
                    - GenAI
                    - LLM
                    - Conceptual
                    - Scenario
                    - Behavioral
                    - Other

                    ========================================================
                    OUTPUT
                    ========================================================

                    Return ONLY valid JSON.

                    Use exactly this structure:

                    {
                      ""IsQuestion"": true,
                      ""IsSkillRelated"": true,
                      ""Question"": ""complete current question text"",
                      ""QuestionType"": ""Coding"",
                      ""Options"": ""A) ...\nB) ...\nC) ...\nD) ..."",
                      ""Reason"": ""brief reason""
                    }

                    If no meaningful question is visible, return:

                    {
                      ""IsQuestion"": false,
                      ""IsSkillRelated"": false,
                      ""Question"": """",
                      ""QuestionType"": """",
                      ""Options"": """",
                      ""Reason"": ""No interview question detected.""
                    }

                    IMPORTANT:

                    Return JSON only.

                    Do not provide the answer.

                    Do not explain your reasoning.

                    Do not reuse a question from a previous screenshot.

                    Treat the current screenshot as completely independent from every
                    previous screenshot and request.
                    ";
        }
    }
}
