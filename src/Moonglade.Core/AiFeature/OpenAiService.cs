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
        private const string Prompt = 
            "你是一个文章读者。下面有一篇博客，你需要阅读这篇博客，对其中的内容进行评论。你的评论尽可能要客观详实，精准的归纳博客的内容，找出其中的优点和核心理念，对核心理念进行鼓励或反对。你需要对博客最大的闪光点进行赞赏，也可以找到可以改进的地方：指出逻辑错误或事实错误（如果有），请详尽的说明是哪些地方有错误。详细的描述这篇文章的改进空间。你的回复会直接发送给博客的作者，因此请尽可能鼓励和肯定作者的写作，并帮助扩展文章的延申内容。你的评论需要和下面博文的语言相同，例如：如果博文是中文，使用中文评论。如果博文是英文，则使用英文进行评论。不要评论政治敏感内容。下面是你要评论的文章内容，请写出一则恰当的博客回复。（无需问候和署名）";

        private const string AbstractPrompt =
            "我刚刚写完了一篇博客，但是我需要为这篇博客写一个摘要。摘要需要能够简明概括这篇博客讲了什么，并且保留一些有趣的问题来吸引读者来阅读、启发读者思考。写一篇好的摘要还需要试图打开读者的思想，让人忍不住对文章的内容进行畅想从而阅读全文，并且能够借文章的内容延伸思考，别忘了摘要的最后可以提出问题吸引读者自己找到答案。我想让你来帮我完成这篇摘要。首先你需要判断文章的编写语言再开始编写摘要。摘要应当讨论文章本身，不要出现'作者'。摘要的长度应当在400字左右，不要超过800字。你的摘要需要和下面博文的语言相同，例如：如果博文是中文，使用中文摘要。如果博文是英文，则使用英文进行摘要。无需问候和署名。只输出写好的摘要，不要输出其它内容。原本的博客文章如下：";
            
        public OpenAiService(
            HttpClient httpClient,
            ILogger<OpenAiService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            _logger = logger;
            _token = configuration["OpenAI:Token"];
            _instance = configuration["OpenAI:Instance"];
        }

        public async Task<string> GenerateComment(string content)
        {
            var response = await Ask(content, Prompt);
            return response.Choices.First().Message.Content;
        }
        
        public async Task<string> GenerateAbstract(string content)
        {
            var response = await Ask(content, AbstractPrompt);
            return response.Choices.First().Message.Content;
        }

        public async Task<CompletionData> Ask(string content, string prompt)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                throw new ArgumentNullException(nameof(_token));
            }

            _logger.LogInformation("Asking OpenAi...");
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
            catch (HttpRequestException raw)
            {
                var remoteError = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(remoteError, raw);
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
        public string Model { get; set; } = "gpt-4";

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
