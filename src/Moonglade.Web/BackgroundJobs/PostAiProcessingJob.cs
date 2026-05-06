using Aiursoft.CSTools.Tools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Core.AiFeature;
using MoongladePure.Core.TagFeature;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;
using System.Text.Json;

namespace MoongladePure.Web.BackgroundJobs
{
    public class PostAiProcessingJob(
        ILogger<PostAiProcessingJob> logger,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env)
        : IHostedService, IDisposable
    {
        private readonly ILogger _logger = logger;
        private const int LengthAiCanProcess = 28000;
        private Timer _timer;

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (env.IsDevelopment() || !EntryExtends.IsProgramEntry())
            {
                _logger.LogInformation("Skip running in development environment");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Post AI Processing job is starting");
            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(25));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Post AI Processing job is stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                _logger.LogInformation("Post AI Processing task started!");
                using (var scope = scopeFactory.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    var configuration = services.GetRequiredService<IConfiguration>();
                    var autoGenerateSummary = configuration.GetValue("OpenAI:AutoGenerateSummary", true);
                    var autoGenerateComment = configuration.GetValue("OpenAI:AutoGenerateComment", true);
                    var openAi = services.GetRequiredService<OpenAiService>();
                    var logger = services.GetRequiredService<ILogger<PostAiProcessingJob>>();
                    var context = services.GetRequiredService<BlogDbContext>();
                    var siteContext = services.GetRequiredService<ISiteContext>();
                    var posts = await context.Post
                        .AsNoTracking()
                        .Where(p => p.SiteId == siteContext.SiteId)
                        .Where(p => p.IsPublished)
                        .Where(p => !p.IsDeleted)
                        .Where(p => p.PubDateUtc >= new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc))
                        .OrderByDescending(p => p.PubDateUtc)
                        .ToListAsync();

                    foreach (var postId in posts.Select(p => p.Id))
                    {
                        // Fetch again. Because this job may run in a long time.
                        var trackedPost = await context.Post.FirstOrDefaultAsync(p => p.SiteId == siteContext.SiteId && p.Id == postId) ??
                                          throw new InvalidOperationException("Failed to locate post with ID: " + postId);

                        // Log.
                        logger.LogInformation("Processing AI for post with slug: {PostSlug}...",
                            trackedPost.Slug);

                        // Generate Chinese abstract
                        if (autoGenerateSummary && (string.IsNullOrWhiteSpace(trackedPost.ContentAbstractZh) || !trackedPost.ContentAbstractZh.EndsWith("--AI Generated")))
                        {
                            var aiJob = await StartAiJob(context, trackedPost, AiJobType.Summary);
                            try
                            {
                                logger.LogInformation("Generating OpenAi Chinese abstract for post with slug: {PostSlug}...",
                                    trackedPost.Slug);
                                var content = trackedPost.RawContent.Length > LengthAiCanProcess
                                    ? trackedPost.RawContent.Substring(trackedPost.RawContent.Length - LengthAiCanProcess, LengthAiCanProcess)
                                    : trackedPost.RawContent;

                                var abstractForPost =
                                    await openAi.GenerateAbstract($"# {trackedPost.Title}" + "\r\n" + content, "Chinese");

                                if (abstractForPost.Length > 1000)
                                {
                                    abstractForPost = abstractForPost[..1000] + "...";
                                }

                                logger.LogInformation("Generated OpenAi Chinese abstract for post with slug: {PostSlug}. New abstract: {Abstract}",
                                    trackedPost.Slug, abstractForPost.SafeSubstring(100));
                                trackedPost.ContentAbstractZh = abstractForPost + "--AI Generated";
                                context.Post.Update(trackedPost);
                                CompleteAiJob(aiJob, AiArtifactType.Summary, "zh-CN", abstractForPost, "ContentAbstractZh");
                                await context.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
                                await FailAiJob(context, aiJob, e);
                                logger.LogCritical(e, "Failed to generate OpenAi Chinese abstract!");
                            }
                            finally
                            {
                                // Sleep to avoid too many requests. Random 0-15 minutes.
                                var minutesToSleep = new Random().Next(0, 15);
                                logger.LogInformation("Sleeping for {Minutes} minutes...", minutesToSleep);
                                await Task.Delay(TimeSpan.FromMinutes(minutesToSleep));
                            }
                        }

                        // Generate English abstract
                        if (autoGenerateSummary && (string.IsNullOrWhiteSpace(trackedPost.ContentAbstractEn) || !trackedPost.ContentAbstractEn.EndsWith("--AI Generated")))
                        {
                            var aiJob = await StartAiJob(context, trackedPost, AiJobType.Summary);
                            try
                            {
                                logger.LogInformation("Generating OpenAi English abstract for post with slug: {PostSlug}...",
                                    trackedPost.Slug);
                                var content = trackedPost.RawContent.Length > LengthAiCanProcess
                                    ? trackedPost.RawContent.Substring(trackedPost.RawContent.Length - LengthAiCanProcess, LengthAiCanProcess)
                                    : trackedPost.RawContent;

                                var abstractForPost =
                                    await openAi.GenerateAbstract($"# {trackedPost.Title}" + "\r\n" + content, "English");

                                if (abstractForPost.Length > 1000)
                                {
                                    abstractForPost = abstractForPost[..1000] + "...";
                                }

                                logger.LogInformation("Generated OpenAi English abstract for post with slug: {PostSlug}. New abstract: {Abstract}",
                                    trackedPost.Slug, abstractForPost.SafeSubstring(100));
                                trackedPost.ContentAbstractEn = abstractForPost + "--AI Generated";
                                context.Post.Update(trackedPost);
                                CompleteAiJob(aiJob, AiArtifactType.Summary, "en-US", abstractForPost, "ContentAbstractEn");
                                await context.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
                                await FailAiJob(context, aiJob, e);
                                logger.LogCritical(e, "Failed to generate OpenAi English abstract!");
                            }
                            finally
                            {
                                // Sleep to avoid too many requests. Random 0-15 minutes.
                                var minutesToSleep = new Random().Next(0, 15);
                                logger.LogInformation("Sleeping for {Minutes} minutes...", minutesToSleep);
                                await Task.Delay(TimeSpan.FromMinutes(minutesToSleep));
                            }
                        }

                        // Delete all obsolete comments. (If multiple comments has the same username, only keep the latest one.)
                        {
                            var allComments = await context.Comment
                                .Where(c => c.SiteId == trackedPost.SiteId)
                                .Where(c => c.PostId == postId)
                                .Where(c => c.IPAddress == "127.0.0.1")
                                .ToListAsync();
                            var obsoleteComments = allComments
                                .GroupBy(c => c.Username)
                                .Where(g => g.Count() > 1)
                                .SelectMany(g => g.OrderByDescending(c => c.CreateTimeUtc).Skip(1))
                                .ToList();
                            if (obsoleteComments.Any())
                            {
                                logger.LogInformation("Deleting obsolete comments for post with slug: {PostSlug}...", trackedPost.Slug);
                            }
                            context.Comment.RemoveRange(obsoleteComments);
                            await context.SaveChangesAsync();
                        }

                        // Get all AI comments.
                        var aiComments = await context.Comment
                            .Where(c => c.SiteId == trackedPost.SiteId)
                            .Where(c => c.PostId == postId)
                            .Where(c => c.IPAddress == "127.0.0.1")
                            .Where(c => c.Username == "Qwen3")
                            .ToListAsync();

                        // Skip valid posts.
                        // ReSharper disable once InvertIf
                        if (autoGenerateComment && !aiComments.Any())
                        {
                            var aiJob = await StartAiJob(context, trackedPost, AiJobType.Comment);
                            try
                            {
                                logger.LogInformation("Generating OpenAi comment for post with slug: {PostSlug}...",
                                    trackedPost.Slug);
                                var content = trackedPost.RawContent.Length > LengthAiCanProcess
                                    ? trackedPost.RawContent.Substring(trackedPost.RawContent.Length - LengthAiCanProcess, LengthAiCanProcess)
                                    : trackedPost.RawContent;

                                var newComment = await openAi.GenerateComment($"# {trackedPost.Title}" + "\r\n" + content);
                                logger.LogInformation("Generated OpenAi comment for post with slug: {PostSlug}. New comment: {Comment}",
                                    trackedPost.Slug, newComment.SafeSubstring(100));
                                await context.Comment.AddAsync(new CommentEntity
                                {
                                    Id = Guid.NewGuid(),
                                    SiteId = trackedPost.SiteId,
                                    PostId = postId,
                                    IPAddress = "127.0.0.1",
                                    Email = "qwen3@alibaba.com",
                                    IsApproved = true,
                                    CommentContent = newComment,
                                    CreateTimeUtc = DateTime.UtcNow,
                                    Username = "Qwen3"
                                });
                                CompleteAiJob(aiJob, AiArtifactType.Comment, null, newComment, "Comment");
                                await context.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
                                await FailAiJob(context, aiJob, e);
                                logger.LogCritical(e, "Failed to generate OpenAi comment!");
                            }
                            finally
                            {
                                // Sleep to avoid too many requests.
                                var minutesToSleep = new Random().Next(0, 15);
                                await Task.Delay(TimeSpan.FromMinutes(minutesToSleep));
                            }
                        }

                        var existingTagsCount = await context.PostTag
                            .Where(pt => pt.SiteId == trackedPost.SiteId)
                            .Where(pt => pt.PostId == postId)
                            .CountAsync();
                        if (existingTagsCount < 6)
                        {
                            var aiJob = await StartAiJob(context, trackedPost, AiJobType.Tags);
                            try
                            {
                                logger.LogInformation("Generating OpenAi tags for post with slug: {PostSlug}...",
                                    trackedPost.Slug);
                                var existingTags = await context.PostTag
                                    .Where(pt => pt.SiteId == trackedPost.SiteId)
                                    .Where(pt => pt.PostId == postId)
                                    .Select(pt => pt.Tag)
                                    .ToListAsync();

                                var newTags = await openAi.GenerateTags(trackedPost.RawContent);
                                CompleteAiJob(aiJob, AiArtifactType.Tags, null, JsonSerializer.Serialize(newTags), "PostTag");
                                var newTagsToAdd = new List<string>();
                                foreach (var newTag in newTags
                                             .Select(t => t.Replace('-', ' ')))
                                {
                                    logger.LogInformation("Generated OpenAi tag for post with slug: {PostSlug}. New tag: '{Tag}'",
                                        trackedPost.Slug, newTag.SafeSubstring(100));
                                    if (existingTags.Any(t =>
                                            string.Equals(t.DisplayName, newTag, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(t.NormalizedName, Tag.NormalizeName(newTag, Helper.TagNormalizationDictionary), StringComparison.OrdinalIgnoreCase)
                                        ))
                                    {
                                        // Not a new tag. Ignore.
                                        logger.LogInformation("Tag already exists. Skipping...");
                                        continue;
                                    }

                                    newTagsToAdd.Add(newTag);
                                }

                                foreach (var newTag in newTagsToAdd.Take(6 - existingTagsCount))
                                {
                                    var newTagNormalized = Tag.NormalizeName(newTag, Helper.TagNormalizationDictionary);

                                    // Create new tag if not exists.
                                    var tag = await context.Tag
                                        .FirstOrDefaultAsync(t => t.SiteId == trackedPost.SiteId && t.NormalizedName == newTagNormalized);
                                    if (tag == null)
                                    {
                                        logger.LogInformation("Creating new tag: '{Tag}' in db...", newTag);
                                        tag = new TagEntity
                                        {
                                            SiteId = trackedPost.SiteId,
                                            DisplayName = newTag,
                                            NormalizedName = newTagNormalized
                                        };
                                        await context.Tag.AddAsync(tag);
                                        await context.SaveChangesAsync();
                                    }

                                    // Add the relation.
                                    logger.LogInformation("Adding tag {Tag} to post {PostSlug}...", newTag, trackedPost.Slug);
                                    await context.PostTag.AddAsync(new PostTagEntity
                                    {
                                        SiteId = trackedPost.SiteId,
                                        PostId = postId,
                                        TagId = tag.Id
                                    });
                                    await context.SaveChangesAsync();
                                }
                                await context.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
                                await FailAiJob(context, aiJob, e);
                                logger.LogCritical(e, "Failed to generate OpenAi tags!");
                            }
                            finally
                            {
                                var minutesToSleep = new Random().Next(0, 15);
                                logger.LogInformation("Sleeping for {Minutes} minutes...", minutesToSleep);
                                await Task.Delay(TimeSpan.FromMinutes(minutesToSleep));
                            }
                        }
                    }
                }

                _logger.LogInformation("Post AI Processing task finished!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Post AI Processing job crashed!");
            }
        }

        private static async Task<AiJobEntity> StartAiJob(BlogDbContext context, PostEntity post, AiJobType jobType)
        {
            var job = new AiJobEntity
            {
                SiteId = post.SiteId,
                JobType = jobType,
                TargetEntityType = nameof(PostEntity),
                TargetEntityId = post.Id,
                Provider = "OpenAI",
                Status = AiJobStatus.Running,
                StartedAtUtc = DateTime.UtcNow
            };

            await context.AiJob.AddAsync(job);
            await context.SaveChangesAsync();
            return job;
        }

        private static void CompleteAiJob(AiJobEntity job, AiArtifactType artifactType, string cultureCode, string content, string legacyTarget)
        {
            job.Status = AiJobStatus.Succeeded;
            job.FinishedAtUtc = DateTime.UtcNow;
            job.Artifacts.Add(new AiArtifactEntity
            {
                SiteId = job.SiteId,
                JobId = job.Id,
                TargetEntityType = job.TargetEntityType,
                TargetEntityId = job.TargetEntityId,
                ArtifactType = artifactType,
                CultureCode = cultureCode,
                Content = content,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    source = nameof(PostAiProcessingJob),
                    legacyTarget
                })
            });
        }

        private static async Task FailAiJob(BlogDbContext context, AiJobEntity job, Exception exception)
        {
            job.Status = AiJobStatus.Failed;
            job.FinishedAtUtc = DateTime.UtcNow;
            job.ErrorMessage = exception.Message.SafeSubstring(2048);
            await context.SaveChangesAsync();
        }
    }
}
