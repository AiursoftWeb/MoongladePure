using Aiursoft.DbTools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Data.Infrastructure;
using MoongladePure.Core.AiFeature;
using MoongladePure.Data;
using MoongladePure.Data.Entities;
using System.Text.RegularExpressions;
using Aiursoft.CSTools.Tools;
using MoongladePure.Web.BackgroundJobs;

public class LangDetectJob(
    IServiceScopeFactory scopeFactory,
    ILogger<LangDetectJob> logger) : IHostedService
{
    private Task _executingTask;
    private readonly CancellationTokenSource _stoppingCts = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _executingTask = ExecuteAsync(_stoppingCts.Token);
        if (_executingTask.IsCompleted)
        {
            return _executingTask;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            return;
        }

        try
        {
            _stoppingCts.Cancel();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWork(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing LangDetectJob.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        logger.LogInformation("LangDetectJob is working.");
        using var scope = scopeFactory.CreateScope();
        var openAi = scope.ServiceProvider.GetRequiredService<OpenAiService>();
        var context = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        // 1. Detect language for posts with missing or invalid language code
        var postsToProcessForLang = await context.Post
            .Where(p => 
                string.IsNullOrEmpty(p.ContentLanguageCode) || 
                p.ContentLanguageCode.Length != 5)
            .OrderByDescending(p => p.PubDateUtc)
            .Take(5)
            .ToListAsync(stoppingToken);

        foreach (var post in postsToProcessForLang)
        {
            if (stoppingToken.IsCancellationRequested) break;
            logger.LogInformation($"Processing post language for: {post.Title}");
            try
            {
                if (string.IsNullOrWhiteSpace(post.RawContent)) continue;

                var language = await openAi.DetectLanguage(post.RawContent, stoppingToken);
                if (!string.IsNullOrWhiteSpace(language) && language.Length <= 8)
                {
                    post.ContentLanguageCode = language;
                    context.Update(post);
                    await context.SaveChangesAsync(stoppingToken);
                    logger.LogInformation($"Updated post '{post.Title}' language to: {language}");
                }
                else
                {
                     logger.LogWarning($"Detected language '{language}' for post '{post.Title}' seems invalid or too long.");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to detect language for post: {post.Title}");
            }
        }

        // 2. Localize posts (Translate)
        // We look for posts that have a valid language code (en-US or zh-CN) AND
        // (LocalizeJobRunAt is null OR LocalizeJobRunAt < LastModifiedUtc)
        // This ensures we only process posts that haven't been localized or have been modified since last localization.
        var postsToLocalize = await context.Post
            .Where(p => p.ContentLanguageCode == "zh-CN" || p.ContentLanguageCode == "en-US")
            .Where(p => p.LocalizeJobRunAt == null || (p.LastModifiedUtc != null && p.LocalizeJobRunAt < p.LastModifiedUtc))
            .OrderByDescending(p => p.PubDateUtc)
            .Take(5)
            .ToListAsync(stoppingToken);

        foreach (var post in postsToLocalize)
        {
            if (stoppingToken.IsCancellationRequested) break;
            logger.LogInformation($"Localizing post: {post.Title} ({post.ContentLanguageCode})");
            try
            {
                if (string.IsNullOrWhiteSpace(post.RawContent)) continue;

                if (post.ContentLanguageCode == "zh-CN")
                {
                    post.LocalizedChineseContent = post.RawContent;
                    // Translate to English
                    var translated = await openAi.Translate(post.RawContent, "English", stoppingToken);
                    post.LocalizedEnglishContent = translated;
                }
                else if (post.ContentLanguageCode == "en-US")
                {
                    post.LocalizedEnglishContent = post.RawContent;
                    // Translate to Chinese
                    var translated = await openAi.Translate(post.RawContent, "Chinese", stoppingToken);
                    post.LocalizedChineseContent = translated;
                }

                post.LocalizeJobRunAt = DateTime.UtcNow;
                context.Update(post);
                await context.SaveChangesAsync(stoppingToken);
                logger.LogInformation($"Localized post '{post.Title}'.");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to localize post: {post.Title}");
            }
        }
    }
}
