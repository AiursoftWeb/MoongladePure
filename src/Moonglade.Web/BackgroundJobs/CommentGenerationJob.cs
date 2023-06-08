using Aiursoft.XelNaga.Tools;
using Microsoft.EntityFrameworkCore;
using MoongladePure.Core.AiFeature;
using MoongladePure.Data.Entities;
using MoongladePure.Data.MySql;

namespace MoongladePure.Web.BackgroundJobs
{
    public class CommentGenerationJob : IHostedService, IDisposable
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public CommentGenerationJob(
            ILogger<CommentGenerationJob> logger,
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
                _logger.LogInformation("Skip running in development environment.");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Comment generator job is starting.");
            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(5), TimeSpan.FromHours(24));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Comment generator job is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            try
            {
                _logger.LogInformation("Comment task started!");
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

                    foreach (var post in posts)
                    {
                        // Clear all GPT-3.5 comments.
                        var gpt35Comments = await context.Comment
                            .Where(c => c.PostId == post.Id)
                            .Where(c => c.IPAddress == "127.0.0.1")
                            .Where(c => c.Username == "ChatGPT")
                            .ToListAsync();
                        context.Comment.RemoveRange(gpt35Comments);
                        await context.SaveChangesAsync();

                        // Get all GPT comments.
                        var chatGptComments = await context.Comment
                            .Where(c => c.PostId == post.Id)
                            .Where(c => c.IPAddress == "127.0.0.1")
                            .Where(c => c.Username == "GPT-4")
                            .ToListAsync();
                        
                        // Skip valid posts.
                        if (chatGptComments.Count == 1)
                        {
                            continue;
                        }

                        // Clear obsolete comments.
                        context.Comment.RemoveRange(chatGptComments);
                        await context.SaveChangesAsync();

                        // Insert a new comment.
                        logger.LogInformation($"Generating ChatGPT's comment for post with slug: {post.Slug}...");
                        try
                        {
                            var content = post.PostContent.Length > 6000 ? 
                                post.PostContent.Substring(post.PostContent.Length - 6000, 6000) :
                                post.PostContent;

                            var newComment = await openAi.GenerateComment($"# {post.Title}" + "\r\n" + content);
                            await context.Comment.AddAsync(new CommentEntity
                            {
                                Id = Guid.NewGuid(),
                                PostId = post.Id,
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

                _logger.LogInformation("Comment generator job task finished!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comment generator job crashed!");
            }
        }
    }
}
