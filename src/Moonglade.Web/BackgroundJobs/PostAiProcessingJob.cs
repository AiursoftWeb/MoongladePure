using Aiursoft.CSTools.Tools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Core.AiFeature;
using MoongladePure.Data.Entities;
using MoongladePure.Data.MySql;

namespace MoongladePure.Web.BackgroundJobs
{
    public class PostAiProcessingJob : IHostedService, IDisposable
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public PostAiProcessingJob(
            ILogger<PostAiProcessingJob> logger,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _env = env;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_env.IsDevelopment() || !EntryExtends.IsProgramEntry())
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
                using (var scope = _scopeFactory.CreateScope())
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
                        
                        if (!trackedPost.ContentAbstract.EndsWith("--GPT 4"))
                        {
                            try
                            {
                                var content = trackedPost.PostContent.Length > 6000
                                    ? trackedPost.PostContent.Substring(trackedPost.PostContent.Length - 6000, 6000)
                                    : trackedPost.PostContent;

                                var abstractForPost =
                                    await openAi.GenerateAbstract($"# {trackedPost.Title}" + "\r\n" + content);

                                if (abstractForPost.Length > 1000)
                                {
                                    abstractForPost = abstractForPost[..1000] + "...";
                                }
                                
                                trackedPost.ContentAbstract = abstractForPost + "--GPT 4";
                                context.Post.Update(trackedPost);
                                await context.SaveChangesAsync();
                            }
                            catch (Exception e)
                            {
                                logger.LogCritical(e, "Failed to generate OpenAi abstract!");
                            }
                            finally
                            {
                                // Sleep to avoid too many requests.
                                await Task.Delay(TimeSpan.FromMinutes(15));
                            }
                        }

                        // Get all GPT comments.
                        var chatGptComments = await context.Comment
                            .Where(c => c.PostId == postId)
                            .Where(c => c.IPAddress == "127.0.0.1")
                            .Where(c => c.Username == "GPT-4")
                            .ToListAsync();

                        // Skip valid posts.
                        // ReSharper disable once InvertIf
                        if (!chatGptComments.Any())
                        {
                            try
                            {
                                var content = trackedPost.PostContent.Length > 6000
                                    ? trackedPost.PostContent.Substring(trackedPost.PostContent.Length - 6000, 6000)
                                    : trackedPost.PostContent;

                                var newComment = await openAi.GenerateComment($"# {trackedPost.Title}" + "\r\n" + content);
                                await context.Comment.AddAsync(new CommentEntity
                                {
                                    Id = Guid.NewGuid(),
                                    PostId = postId,
                                    IPAddress = "127.0.0.1",
                                    Email = "chatgpt@domain.com",
                                    IsApproved = true,
                                    CommentContent = newComment,
                                    CreateTimeUtc = DateTime.UtcNow,
                                    Username = "GPT-4"
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
                                await Task.Delay(TimeSpan.FromMinutes(30));
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
