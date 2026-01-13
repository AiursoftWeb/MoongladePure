using Aiursoft.CSTools.Tools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Core.AiFeature;
using System.Text.RegularExpressions;

namespace MoongladePure.Web.BackgroundJobs;

public class LangDetectJob(
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment env,
    ILogger<LangDetectJob> logger) : IHostedService, IDisposable
{
    private Timer _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (env.IsDevelopment() || !EntryExtends.IsProgramEntry())
        {
            logger.LogInformation("Skip running in development environment");
            return Task.CompletedTask;
        }

        logger.LogInformation("LangDetectJob is starting");
        _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(15));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("LangDetectJob is stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private const string LanguageRegexPattern = "^[a-z]{2}-[A-Z]{2}$";

    private async void DoWork(object state)
    {
        var localizeJobStartTime =  DateTime.UtcNow;
        try
        {
            logger.LogInformation("LangDetectJob started!");
            using var scope = scopeFactory.CreateScope();
            var openAi = scope.ServiceProvider.GetRequiredService<OpenAiService>();
            var context = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

            // 1. Detect language for posts with missing or invalid language code
            // 1. Detect language for posts with missing or invalid language code
            // WE MUST Fetch all posts' language codes to correct them.
            // Because Regex is not supported in LINQ, we fetch all IDs and LanguageCodes, check them in memory,
            // and then pick the ones that need processing.
            var allPostLanguages = await context.Post
                .Select(p => new { p.Id, p.ContentLanguageCode, p.Title, p.PubDateUtc })
                .OrderByDescending(p => p.PubDateUtc)
                .ToListAsync();

            var invalidPostIds = allPostLanguages
                .Where(p => string.IsNullOrEmpty(p.ContentLanguageCode) || !Regex.IsMatch(p.ContentLanguageCode, LanguageRegexPattern))
                .Select(p => p.Id)
                .Take(5)
                .ToList();

            var postsToProcessForLang = await context.Post
                .Where(p => invalidPostIds.Contains(p.Id))
                .ToListAsync();

            foreach (var post in postsToProcessForLang)
            {
                logger.LogInformation($"Processing post language for: {post.Title}");
                try
                {
                    if (string.IsNullOrWhiteSpace(post.RawContent)) continue;

                    string language = null;
                    for (int i = 0; i < 3; i++)
                    {
                        language = await openAi.DetectLanguage(post.RawContent);
                        if (!string.IsNullOrWhiteSpace(language) && Regex.IsMatch(language, LanguageRegexPattern))
                        {
                            break;
                        }
                        logger.LogWarning($"Attempt {i + 1}: Invalid language code '{language}' detected for post '{post.Title}'. Retrying...");
                        language = null; // Reset if invalid
                    }

                    if (!string.IsNullOrWhiteSpace(language))
                    {
                        post.ContentLanguageCode = language;
                        context.Update(post);
                        await context.SaveChangesAsync();
                        logger.LogInformation($"Updated post '{post.Title}' language to: {language}");
                    }
                    else
                    {
                        logger.LogWarning($"Failed to detect valid language for post '{post.Title}' after 3 attempts.");
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Failed to detect language for post: {post.Title}");
                }
            }

            // 2. Localize posts (Translate)
            var postsToLocalize = await context.Post
                .Where(p =>
                    p.LocalizeJobRunAt == null ||
                    (p.LocalizedChineseContent == null || p.LocalizedChineseContent == "") ||
                    (p.LocalizedEnglishContent == null || p.LocalizedEnglishContent == "") ||
                    (p.LastModifiedUtc != null && p.LocalizeJobRunAt < p.LastModifiedUtc))
                .OrderByDescending(p => p.PubDateUtc)
                .Take(5)
                .ToListAsync();

            foreach (var post in postsToLocalize)
            {
                logger.LogInformation($"Localizing post: {post.Title} ({post.ContentLanguageCode})");
                try
                {
                    if (string.IsNullOrWhiteSpace(post.RawContent)) continue;

                    if (post.ContentLanguageCode == "zh-CN")
                    {
                        post.LocalizedChineseContent = post.RawContent;
                        // Translate to English
                        var translated = await openAi.Translate(post.RawContent, "English");
                        post.LocalizedEnglishContent = translated;
                    }
                    else if (post.ContentLanguageCode == "en-US")
                    {
                        post.LocalizedEnglishContent = post.RawContent;
                        // Translate to Chinese
                        var translated = await openAi.Translate(post.RawContent, "Chinese");
                        post.LocalizedChineseContent = translated;
                    }
                    else
                    {
                        post.LocalizedEnglishContent = await openAi.Translate(post.RawContent, "English");
                        post.LocalizedChineseContent = await openAi.Translate(post.RawContent, "Chinese");
                    }

                    post.LocalizeJobRunAt = localizeJobStartTime;
                    context.Update(post);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Localized post '{post.Title}'.");
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Failed to localize post: {post.Title}");
                }
            }
            logger.LogInformation("LangDetectJob finished!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LangDetectJob crashed!");
        }
    }
}
