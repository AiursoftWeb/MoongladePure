using Aiursoft.CSTools.Tools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Core.AiFeature;
using MoongladePure.Data.Entities;
using MoongladePure.Data.MySql;

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
                    var context = services.GetRequiredService<MySqlBlogDbContext>();
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
                                var content = trackedPost.PostContent.Length > LengthAiCanProcess
                                    ? trackedPost.PostContent.Substring(trackedPost.PostContent.Length - LengthAiCanProcess, LengthAiCanProcess)
                                    : trackedPost.PostContent;

                                var abstractForPost =
                                    await openAi.GenerateAbstract($"# {trackedPost.Title}" + "\r\n" + content);

                                if (abstractForPost.Length > 1000)
                                {
                                    abstractForPost = abstractForPost[..1000] + "...";
                                }
                                
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
                                await Task.Delay(TimeSpan.FromMinutes(minutesToSleep));
                            }
                        }
                        
                        // Delete all obsolete comments. (Username contains "R1")
                        var obsoleteComments = await context.Comment
                            .Where(c => c.PostId == postId)
                            .Where(c => c.IPAddress == "127.0.0.1")
                            .Where(c => c.Username.Contains("R1"))
                            .ToListAsync();
                        context.Comment.RemoveRange(obsoleteComments);
                        await context.SaveChangesAsync();

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
                                var content = trackedPost.PostContent.Length > LengthAiCanProcess
                                    ? trackedPost.PostContent.Substring(trackedPost.PostContent.Length - LengthAiCanProcess, LengthAiCanProcess)
                                    : trackedPost.PostContent;

                                var newComment = await openAi.GenerateComment($"# {trackedPost.Title}" + "\r\n" + content);
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
