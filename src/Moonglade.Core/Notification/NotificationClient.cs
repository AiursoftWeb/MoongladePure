﻿using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moonglade.Configuration.Abstraction;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Pingback;

namespace Moonglade.Core.Notification
{
    public class NotificationClient : IBlogNotificationClient
    {
        private readonly HttpClient _httpClient;

        public bool IsEnabled { get; set; }

        private readonly ILogger<NotificationClient> _logger;

        private readonly IBlogConfig _blogConfig;

        public NotificationClient(
            ILogger<NotificationClient> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            HttpClient httpClient)
        {
            _logger = logger;
            _blogConfig = blogConfig;
            if (settings.Value.Notification.Enabled)
            {
                if (Uri.IsWellFormedUriString(settings.Value.Notification.AzureFunctionEndpoint, UriKind.Absolute))
                {
                    httpClient.BaseAddress = new Uri(settings.Value.Notification.AzureFunctionEndpoint);
                }
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"Moonglade/{Utils.AppVersion}");
                _httpClient = httpClient;

                if (_blogConfig.NotificationSettings.EnableEmailSending)
                {
                    IsEnabled = true;
                }
            }
        }

        public async Task TestNotificationAsync()
        {
            if (!IsEnabled)
            {
                _logger.LogWarning($"Skipped {nameof(TestNotificationAsync)} because Email sending is disabled.");
                return;
            }

            try
            {
                var req = BuildNotificationRequest(() =>
                    new NotificationRequest<EmptyPayload>(MailMesageTypes.TestMail, EmptyPayload.Default));
                var response = await _httpClient.SendAsync(req);

                if (response.IsSuccessStatusCode)
                {
                    var dataStr = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Test email is sent, server response: '{dataStr}'");
                }
                else
                {
                    throw new Exception($"Test email sending failed, response code: '{response.StatusCode}'");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task NotifyCommentAsync(CommentDetailedItem model, Func<string, string> contentFormat)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning($"Skipped {nameof(NotifyCommentAsync)} because Email sending is disabled.");
                return;
            }

            try
            {
                var req = new CommentPayload(
                    model.Username,
                    model.Email,
                    model.IpAddress,
                    model.PostTitle,
                    contentFormat(model.CommentContent),
                    model.CreateOnUtc
                );

                await SendNotificationRequest(
                    new NotificationRequest<CommentPayload>(MailMesageTypes.NewCommentNotification, req));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task NotifyCommentReplyAsync(CommentReplyDetail model, string postLink)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning($"Skipped {nameof(NotifyCommentReplyAsync)} because Email sending is disabled.");
                return;
            }

            try
            {
                var req = new CommentReplyPayload(
                    model.Email,
                    model.CommentContent,
                    model.Title,
                    model.ReplyContentHtml,
                    postLink);

                await SendNotificationRequest(
                    new NotificationRequest<CommentReplyPayload>(MailMesageTypes.AdminReplyNotification, req));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task NotifyPingbackAsync(PingbackHistory model)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning($"Skipped {nameof(NotifyPingbackAsync)} because Email sending is disabled.");
                return;
            }

            try
            {
                var req = new PingPayload(
                    model.TargetPostTitle,
                    model.PingTimeUtc,
                    model.Domain,
                    model.SourceIp,
                    model.SourceUrl,
                    model.SourceTitle);

                await SendNotificationRequest(new NotificationRequest<PingPayload>(MailMesageTypes.BeingPinged, req));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private async Task SendNotificationRequest<T>(NotificationRequest<T> request, [CallerMemberName] string callerMemberName = "") where T : class
        {
            var req = BuildNotificationRequest(() => request);
            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error executing request '{callerMemberName}', response: {response.StatusCode}");
            }
        }

        private HttpRequestMessage BuildNotificationRequest<T>(Func<NotificationRequest<T>> request) where T : class
        {
            var nf = request();
            nf.EmailDisplayName = _blogConfig.NotificationSettings.EmailDisplayName;
            nf.AdminEmail = _blogConfig.NotificationSettings.AdminEmail;

            var req = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = new NotificationContent<T>(nf)
            };
            return req;
        }
    }
}