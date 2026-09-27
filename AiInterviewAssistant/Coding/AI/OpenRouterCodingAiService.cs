using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AiInterviewAssistant.Coding.AI
{
    public sealed class OpenRouterCodingAiService : ICodingAiService
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private readonly string model;

        public OpenRouterCodingAiService(
            HttpClient httpClient,
            string apiKey,
            string model)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            this.apiKey = apiKey;
            this.model = string.IsNullOrWhiteSpace(model)
                ? "google/gemini-2.5-flash-lite"
                : model;
        }

        public async Task<CodingAiResponse> GenerateAnswerAsync(
            CodingAiRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return new CodingAiResponse
                {
                    Success = false,
                    ErrorMessage = "Coding AI request is null."
                };
            }

            try
            {
                string prompt = BuildPrompt(request);

                var payload = new
                {
                    model = model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content =
                                "You are a coding interview assistant. " +
                                "Return the complete answer. " +
                                "For coding questions, provide complete compilable code. " +
                                "Do not truncate the code. " +
                                "Do not add unnecessary explanation."
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.1
                };

                string json =
                    JsonConvert.SerializeObject(payload);

                using (var content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"))
                {
                    using (HttpRequestMessage requestMessage =
                        new HttpRequestMessage(
                            HttpMethod.Post,
                            "https://openrouter.ai/api/v1/chat/completions"))
                    {
                        requestMessage.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue(
                                "Bearer",
                                apiKey);

                        requestMessage.Content = content;

                        using (HttpResponseMessage response =
                            await httpClient.SendAsync(
                                requestMessage,
                                cancellationToken))
                        {
                            string responseText =
                                await response.Content.ReadAsStringAsync();

                            if (!response.IsSuccessStatusCode)
                            {
                                return new CodingAiResponse
                                {
                                    Success = false,
                                    ErrorMessage =
                                        responseText
                                };
                            }

                            dynamic result =
                                JsonConvert.DeserializeObject(
                                    responseText);

                            string answer =
                                result?
                                    .choices?[0]?
                                    .message?
                                    .content?
                                    .ToString();

                            if (string.IsNullOrWhiteSpace(answer))
                            {
                                return new CodingAiResponse
                                {
                                    Success = false,
                                    ErrorMessage =
                                        "Empty response received from coding AI."
                                };
                            }

                            return new CodingAiResponse
                            {
                                Success = true,
                                Answer = answer
                            };
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new CodingAiResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private string BuildPrompt(
    CodingAiRequest request)
        {
            var builder =
                new StringBuilder();

            builder.AppendLine(
                "You are solving a coding question extracted directly from an online coding platform.");

            builder.AppendLine(
                "The extracted problem statement and code editor content below are the source of truth.");

            builder.AppendLine(
                "Do NOT say that the question cannot be identified if any meaningful problem statement, examples, constraints, or starter code is present.");

            builder.AppendLine(
                "Infer the required solution from the extracted content when formatting is incomplete.");

            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(request.Question))
            {
                builder.AppendLine("=== CODING QUESTION ===");
                builder.AppendLine(request.Question);
                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.Input))
            {
                builder.AppendLine("=== INPUT FORMAT ===");
                builder.AppendLine(request.Input);
                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.Output))
            {
                builder.AppendLine("=== OUTPUT FORMAT ===");
                builder.AppendLine(request.Output);
                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.Constraints))
            {
                builder.AppendLine("=== CONSTRAINTS ===");
                builder.AppendLine(request.Constraints);
                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.Examples))
            {
                builder.AppendLine("=== EXAMPLES ===");
                builder.AppendLine(request.Examples);
                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.StarterCode))
            {
                builder.AppendLine("=== CODE EDITOR / STARTER CODE ===");
                builder.AppendLine(request.StarterCode);
                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.Language))
            {
                builder.AppendLine("=== PROGRAMMING LANGUAGE ===");
                builder.AppendLine(request.Language);
                builder.AppendLine();
            }

            builder.AppendLine("=== REQUIRED RESPONSE ===");
            builder.AppendLine(
                "Return the complete solution code that can be submitted to the coding platform.");

            builder.AppendLine(
                "Preserve the required function/class/method signature from the starter code when one is provided.");

            builder.AppendLine(
                "Do not return 'Unable to identify the question' unless the extracted content is completely empty.");

            builder.AppendLine(
                "Do not truncate the solution.");

            builder.AppendLine(
                "Prefer code only unless the platform requires another response format.");

            return builder.ToString();
        }
    }
}