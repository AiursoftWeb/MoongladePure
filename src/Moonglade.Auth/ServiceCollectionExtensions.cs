using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MoongladePure.Data.Entities;
using MoongladePure.Data.Infrastructure;

namespace MoongladePure.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlogAuthenticaton(this IServiceCollection services,
        IConfiguration configuration)
    {
        var authProvider = configuration.GetValue<string>("AppSettings:AuthProvider");

        if (authProvider == "OIDC")
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/SignIn";
                    options.LogoutPath = "/auth/signout";
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    var oidcConfig = configuration.GetSection("OIDC");

                    options.Authority = oidcConfig["Authority"];
                    options.ClientId = oidcConfig["ClientId"];
                    options.ClientSecret = oidcConfig["ClientSecret"];
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");

                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.ResponseType = OpenIdConnectResponseType.Code;

                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.TokenValidationParameters.RoleClaimType = "groups";
                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();
                            Console.WriteLine("OnTokenValidated. Got claims:");
                            foreach (var c in context.Principal!.Claims)
                            {
                                Console.WriteLine($"  {c.Type} => {c.Value}");
                            }

                            // 从OIDC的claims中提取关键信息
                            var name = context.Principal!.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                                       ?? context.Principal!.FindFirst("name")?.Value;

                            if (string.IsNullOrEmpty(name))
                            {
                                context.Fail("Name claim not found in OIDC token.");
                                return;
                            }

                            // 创建或同步用户，并获取本地用户的UID
                            var uid = await mediator.Send(new OidcUserSyncCommand(name));

                            // 清除OIDC的旧claims，添加我们自己的claims
                            var identity = (ClaimsIdentity)context.Principal.Identity;
                            identity!.RemoveClaim(identity.FindFirst(ClaimTypes.NameIdentifier)); // 移除OIDC的sub
                            identity.AddClaim(new("uid", uid.ToString()));
                            identity.AddClaim(new(ClaimTypes.Role, "Administrator"));
                        }
                    };
                });
        }
        else // 默认为 "Local" 认证
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.AccessDeniedPath = "/auth/accessdenied";
                    options.LoginPath = "/auth/signin";
                    options.LogoutPath = "/auth/signout";
                });
        }

        return services;
    }
}


// Command: 包含从OIDC获取的必要信息
public record OidcUserSyncCommand(string Username) : IRequest<Guid>;

// Handler: 处理用户同步的核心逻辑
public class OidcUserSyncCommandHandler(IRepository<LocalAccountEntity> repo)
    : IRequestHandler<OidcUserSyncCommand, Guid>
{
    public async Task<Guid> Handle(OidcUserSyncCommand request, CancellationToken ct)
    {
        // OIDC用户我们以Username为唯一标识
        var account = await repo.GetAsync(p => p.Username == request.Username);

        if (account is not null)
        {
            // 用户已存在，直接返回ID
            return account.Id;
        }

        // 用户不存在，创建一个新用户
        var newAccount = new LocalAccountEntity
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            CreateTimeUtc = DateTime.UtcNow,
            PasswordHash = "OIDC_USER",
            PasswordSalt = string.Empty
        };

        await repo.AddAsync(newAccount, ct);
        return newAccount.Id;
    }
}
