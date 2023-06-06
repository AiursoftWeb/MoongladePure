using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MoongladePure.Core.AiFeature
{
    public class OpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _token;
        private readonly string _instance;
        private const string _prompt = "你是一个文章评论员。下面有一篇博客，你需要阅读这篇博客，对其中的内容进行评论。你的评论尽可能要客观详实，精准的总结博客的内容，找出其中的优点和缺点，找到其核心理念，对核心理念进行鼓励或反对。你需要找到博客最大的闪光点进行赞赏，也需要找到可以改进的地方：指出逻辑错误或事实错误，请详尽的说明是哪些地方有错误。详细的描述这篇文章的改进空间。你的回复会直接发送给博客的作者，因此请尽可能鼓励和肯定作者的写作，并帮助扩展文章的延申内容。你的评论需要和博文的语言相同，例如：如果博文是中文，使用中文评论。如果博文是英文，则使用英文进行评论。下面是你要评论的文章内容：";

        public OpenAiService(
            HttpClient httpClient,
            ILogger<OpenAiService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _token = configuration["OpenAI:Token"];
            _instance = configuration["OpenAI:Instance"];
        }

        public async Task<string> GenerateComment(string content)
        {
            var response = await Ask(content, _prompt);
            return response.Choices.First().Message.Content;
        }

        public async Task<CompletionData> Ask(string content, string prompt)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                throw new ArgumentNullException(nameof(_token));
            }

            _logger.LogInformation("Asking OpenAi to generate a comment...");
            var model = new OpenAiModel
            {
                Messages = new List<MessagesItem>
                {
                    new()
                    {
                        Content = prompt,
                        Role = "user"
                    },
                    new()
                    {
                        Content = content,
                        Role = "user"
                    }
                }
            };

            var json = JsonSerializer.Serialize(model);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_instance}/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {_token}");
            var response = await _httpClient.SendAsync(request);
            try
            {
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();
                var responseModel = JsonSerializer.Deserialize<CompletionData>(responseJson);
                return responseModel;
            }
            catch (HttpRequestException e)
            {
                _logger.LogCritical(e, "Crashed when calling OpenAI API!");
                var remoteError = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(remoteError);
            }
        }
    }

    public class MessagesItem
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class OpenAiModel
    {
        [JsonPropertyName("messages")]
        public List<MessagesItem> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("model")] 
        public string Model { get; set; } = "gpt-3.5-turbo";

        [JsonPropertyName("temperature")] 
        public double Temperature { get; set; } = 0.5;

        [JsonPropertyName("presence_penalty")] 
        public int PresencePenalty { get; set; } = 0;
    }

    public class UsageData
    {
        /// <summary>
        /// The number of prompt tokens used in the request.
        /// </summary>
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        /// <summary>
        /// The number of completion tokens generated in the response.
        /// </summary>
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        /// <summary>
        /// The total number of tokens used in the request and generated in the response.
        /// </summary>
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        /// <summary>
        /// The number of tokens in the prompt before any adjustments were made.
        /// </summary>
        [JsonPropertyName("pre_token_count")]
        public int PreTokenCount { get; set; }

        /// <summary>
        /// The total number of tokens in the prompt before any adjustments were made.
        /// </summary>
        [JsonPropertyName("pre_total")]
        public int PreTotal { get; set; }

        /// <summary>
        /// The total number of tokens used in the response after adjustments were made.
        /// </summary>
        [JsonPropertyName("adjust_total")]
        public int AdjustTotal { get; set; }

        /// <summary>
        /// The final total number of tokens in the response.
        /// </summary>
        [JsonPropertyName("final_total")]
        public int FinalTotal { get; set; }
    }

    public class MessageData
    {
        /// <summary>
        /// The role of the message, such as "user" or "bot".
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class ChoicesItemData
    {
        /// <summary>
        /// The message data for this choice.
        /// </summary>
        [JsonPropertyName("message")]
        public MessageData Message { get; set; }

        /// <summary>
        /// The reason why this choice was selected as the final choice.
        /// </summary>
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }

        /// <summary>
        /// The index of this choice in the list of choices.
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class CompletionData
    {
        /// <summary>
        /// The ID of the completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// The type of the object, which is always "text_completion".
        /// </summary>
        [JsonPropertyName("object")]
        public string Object { get; set; }

        /// <summary>
        /// The timestamp when the completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public int Created { get; set; }

        /// <summary>
        /// The name of the model used to generate the completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// The usage data for this completion.
        /// </summary>
        [JsonPropertyName("usage")]
        public UsageData Usage { get; set; }

        /// <summary>
        /// The list of choices generated by the completion.
        /// </summary>
        [JsonPropertyName("choices")]
        // ReSharper disable once CollectionNeverUpdated.Global
        public List<ChoicesItemData> Choices { get; set; } = new List<ChoicesItemData>();
    }

    /// <summary>
    /// Represents the response data from the OpenAI API for a text completion request.
    /// </summary>
    public class TextCompletionResponseData
    {
        /// <summary>
        /// The completion data for this response.
        /// </summary>
        [JsonPropertyName("completion")]
        public CompletionData Completion { get; set; }
    }
}
