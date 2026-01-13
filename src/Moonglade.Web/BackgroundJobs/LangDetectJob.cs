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

        // We look for posts that have no language code, or the language code is not in the format of "xx-XX" (length 5).
        // This avoids processing "fr-FR" or "de-DE" repeatedly, while correctly picking up "English" or "en" or empty ones.
        var postsToProcess = await context.Post
            .Where(p => 
                string.IsNullOrEmpty(p.ContentLanguageCode) || 
                p.ContentLanguageCode.Length != 5)
            .OrderByDescending(p => p.PubDateUtc)
            .Take(5)
            .ToListAsync(stoppingToken);

        foreach (var post in postsToProcess)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            logger.LogInformation($"Processing post language for: {post.Title}");
            try
            {
                // Must have RawContent to detect language
                if (string.IsNullOrWhiteSpace(post.RawContent))
                {
                    continue;
                }

                var language = await openAi.DetectLanguage(post.RawContent, stoppingToken);

                // Sanitize language code if needed, but OpenAI usually returns BCP 47
                // simple validation
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
    }
}
