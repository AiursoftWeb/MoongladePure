using System.Text.RegularExpressions;
using Aiursoft.Canon;
using Aiursoft.GptClient.Abstractions;
using Aiursoft.GptClient.Services;
using Microsoft.Extensions.Configuration;

namespace MoongladePure.Core.AiFeature;

public class OpenAiService(
    RetryEngine retryEngine,
    ChatClient chatClient,
    IConfiguration configuration)
{
    private readonly GptModel _model = Enum.Parse<GptModel>(configuration["OpenAI:Model"]!);

    private const string Prompt =
        "你是一个文章读者。下面有一篇博客，你需要阅读这篇博客，对其中的内容进行评论。你的评论尽可能要客观详实，精准的归纳博客的内容，找出其中的优点和核心理念，对核心理念进行鼓励或反对。你需要对博客最大的闪光点进行赞赏，也可以找到可以改进的地方：指出逻辑错误或事实错误（如果有），请详尽的说明是哪些地方有错误。详细的描述这篇文章的改进空间。你的回复会直接发送给博客的作者，因此请尽可能鼓励和肯定作者的写作，并帮助扩展文章的延申内容。你的评论需要和下面博文的语言相同，例如：如果博文是中文，使用中文评论。如果博文是英文，则使用英文进行评论。不要评论政治敏感内容。下面是你要评论的文章内容，不要重复输出文章内容，只根据文章内容写出一篇恰当的博客回复和作者讨论。（无需问候和署名，不要使用标题）原文如下：";

    private const string WorkPrompt = "好了，根据上面的文章，现在开始你的评论工作吧！别忘了，不要重复输出文章内容，只根据文章内容写出一篇恰当的博客回复和作者讨论。（无需问候和署名，不要使用标题）";

    private const string AbstractPrompt =
        "我刚刚写完了一篇博客，但是我需要为这篇博客写一个摘要。摘要需要能够简明概括这篇博客讲了什么，并且保留一些有趣的问题来吸引读者来阅读、启发读者思考。写一篇好的摘要还需要试图打开读者的思想，让人忍不住对文章的内容进行畅想从而阅读全文，并且能够借文章的内容延伸思考，别忘了摘要的最后可以提出问题吸引读者自己找到答案。我想让你来帮我完成这篇摘要。首先你需要判断文章的编写语言再开始编写摘要。摘要应当讨论文章本身，不要出现'作者'。摘要的长度应当非常精简，在500字左右，不要超过700字。你的摘要需要和下面博文的语言相同，例如：如果博文是中文，使用中文摘要。如果博文是英文，则使用英文进行摘要。无需问候和署名。**不要**使用markdown！**不要**分段！！！你是在做摘要而不要重新反复复述文章的内容。只输出写好的摘要！！！不要输出其它内容。不要强调你的摘要的特点。原本的博客文章如下：";

    private const string WorkAbstractPrompt =
        "好了，根据上面的文章，现在开始你的摘要工作吧！别忘了，无需问候和署名。**不要**使用markdown！**不要**分段！！！你是在做摘要而不要重新反复复述文章的内容。只输出写好的摘要！！！不要输出其它内容。不要强调你的摘要的特点。";

    private const string TagsPrompt =
        "我刚刚写完了一篇博客，但是我需要为这篇博客写六个 Tag。Tag 是一种关键词，用来描述这篇博客的主题。Tag 需要简洁明了，能够准确描述这篇博客的主题。Tag 之间用逗号分隔。优秀的 Tag 可以方便搜索引擎更好的索引这篇博客，也可以让读者更好的了解这篇博客的主题。我想让你来帮我完成这六个 Tag。Tag 的数量应当为六个，不要多也不要少。Tag 的长度应当非常精简，不要超过 20 个字符。你的 Tag 需要使用英文。原本的博客文章如下：";

    private const string WorkTagsPrompt =
        "好了，根据上面的文章，现在开始你的 Tag 工作吧！别忘了，Tag 的数量应当为六个，不要多也不要少。Tag 的长度应当非常精简，不要超过 20 个字符。你的 Tag 需要使用英文。Tag要尊重商标的正确大小写。例如 'RISC-V'，'.NET'。注意，你输出的 Tags 必须按照如下格式：<tag1>Some tag</tag1>, <tag2>Another Tag</tag2>, <tag3>Another Tag</tag3>, <tag4>Another Tag</tag4>, <tag5>Another Tag</tag5>, <tag6>Last Tag</tag6>";
    
    public async Task<string> GenerateComment(string content)
    {
        var response = await Ask(
            $"""
             {Prompt}

             =====================
             {content}
             =====================

             {WorkPrompt}
             """);
        return response.GetAnswerPart();
    }

    public async Task<string> GenerateAbstract(string content)
    {
        var response = await Ask(
            $"""
             {AbstractPrompt}

             =====================
             {content}
             =====================

             {WorkAbstractPrompt}
             """);
        return response.GetAnswerPart();
    }

    private Task<CompletionData> Ask(params string[] content)
    {
        return chatClient.AskString(_model, content);
    }

    public Task<string[]> GenerateTags(string trackedPostPostContent)
    {
        return retryEngine.RunWithRetry(
            attempts: 8,
            taskFactory: async _ =>
            {
                var response = await Ask(
                    $"""
                     {TagsPrompt}

                     =====================
                     {trackedPostPostContent}
                     =====================

                     {WorkTagsPrompt}
                     """);
                
                var answer = response.GetAnswerPart();
                
                var regex = new Regex(@"<tag\d+>(.*?)</tag\d+>", RegexOptions.IgnoreCase);
                var matches = regex.Matches(answer);
            
                if (matches.Count < 6)
                {
                    throw new InvalidOperationException("Unable to find 6 tags in the response.");
                }

                var tags = new string[6];
                for (var i = 0; i < 6; i++)
                {
                    tags[i] = matches[i].Groups[1].Value;
                }

                return tags;
                
            });
    }
}