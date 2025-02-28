using Aiursoft.CSTools.Tools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Core.AiFeature;
using MoongladePure.Core.TagFeature;
using MoongladePure.Data.Entities;

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
                    var openAi = services.GetRequiredService<OpenAiService>();
                    var logger = services.GetRequiredService<ILogger<Startup>>();
                    var context = services.GetRequiredService<BlogDbContext>();
                    var posts = await context.Post
                        .AsNoTracking()
                        .Where(p => p.IsPublished)
                        .Where(p => !p.IsDeleted)
                        .OrderByDescending(p => p.PubDateUtc)
                        .ToListAsync();

                    foreach (var postId in posts.Select(p => p.Id))
                    {
                        // Fetch again. Because this job may run in a long time.
                        var trackedPost = await context.Post.FindAsync(postId) ??
                                          throw new InvalidOperationException("Failed to locate post with ID: " + postId);

                        // Log.
                        logger.LogInformation("Processing AI for post with slug: {PostSlug}...",
                            trackedPost.Slug);

                        if (!trackedPost.ContentAbstract.EndsWith("--DeepSeek"))
                        {
                            try
                            {
                                logger.LogInformation("Generating OpenAi abstract for post with slug: {PostSlug}...",
                                    trackedPost.Slug);
                                var content = trackedPost.PostContent.Length > LengthAiCanProcess
                                    ? trackedPost.PostContent.Substring(trackedPost.PostContent.Length - LengthAiCanProcess, LengthAiCanProcess)
                                    : trackedPost.PostContent;

                                var abstractForPost =
                                    await openAi.GenerateAbstract($"# {trackedPost.Title}" + "\r\n" + content);

                                if (abstractForPost.Length > 1000)
                                {
                                    abstractForPost = abstractForPost[..1000] + "...";
                                }

                                logger.LogInformation("Generated OpenAi abstract for post with slug: {PostSlug}. New abstract: {Abstract}",
                                    trackedPost.Slug, abstractForPost.SafeSubstring(100));
                                trackedPost.ContentAbstract = abstractForPost + "--DeepSeek";
                                context.Post.Update(trackedPost);
                                await context.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
                                logger.LogCritical(e, "Failed to generate OpenAi abstract!");
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
                            .Where(c => c.PostId == postId)
                            .Where(c => c.IPAddress == "127.0.0.1")
                            .Where(c => c.Username == "DeepSeek")
                            .ToListAsync();

                        // Skip valid posts.
                        // ReSharper disable once InvertIf
                        if (!aiComments.Any())
                        {
                            try
                            {
                                logger.LogInformation("Generating OpenAi comment for post with slug: {PostSlug}...",
                                    trackedPost.Slug);
                                var content = trackedPost.PostContent.Length > LengthAiCanProcess
                                    ? trackedPost.PostContent.Substring(trackedPost.PostContent.Length - LengthAiCanProcess, LengthAiCanProcess)
                                    : trackedPost.PostContent;

                                var newComment = await openAi.GenerateComment($"# {trackedPost.Title}" + "\r\n" + content);
                                logger.LogInformation("Generated OpenAi comment for post with slug: {PostSlug}. New comment: {Comment}",
                                    trackedPost.Slug, newComment.SafeSubstring(100));
                                await context.Comment.AddAsync(new CommentEntity
                                {
                                    Id = Guid.NewGuid(),
                                    PostId = postId,
                                    IPAddress = "127.0.0.1",
                                    Email = "service@deepseek.com",
                                    IsApproved = true,
                                    CommentContent = newComment,
                                    CreateTimeUtc = DateTime.UtcNow,
                                    Username = "DeepSeek"
                                });
                                await context.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
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
                            .Where(pt => pt.PostId == postId)
                            .CountAsync();
                        if (existingTagsCount < 6)
                        {
                            logger.LogInformation("Generating OpenAi tags for post with slug: {PostSlug}...",
                                trackedPost.Slug);
                            var existingTags = await context.PostTag
                                .Where(pt => pt.PostId == postId)
                                .Select(pt => pt.Tag)
                                .ToListAsync();

                            var newTags = await openAi.GenerateTags(trackedPost.PostContent);
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
                                    .FirstOrDefaultAsync(t => t.NormalizedName == newTagNormalized);
                                if (tag == null)
                                {
                                    logger.LogInformation("Creating new tag: '{Tag}' in db...", newTag);
                                    tag = new TagEntity
                                    {
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
                                    PostId = postId,
                                    TagId = tag.Id
                                });
                                await context.SaveChangesAsync();
                            }

                            var minutesToSleep = new Random().Next(0, 15);
                            logger.LogInformation("Sleeping for {Minutes} minutes...", minutesToSleep);
                            await Task.Delay(TimeSpan.FromMinutes(minutesToSleep));
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
    }
}
