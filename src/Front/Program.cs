using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProxyKit;
using Sample.Front.Middleware;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddProxy();
        builder.Services.AddRazorPages();
        builder.Services.AddAccessTokenManagement(options => options.User.RefreshBeforeExpiration = TimeSpan.FromSeconds(1));
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => 
            {
                options.Cookie.Name = "bff-cookie";
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Events.OnSigningOut = e => e.HttpContext.RevokeUserRefreshTokenAsync();
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = builder.Configuration["IDP:Authority"];
                options.ClientId = "internal-api";
                options.ClientSecret = builder.Configuration["IDP:Secret"];
                options.ResponseType = "code";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("internal-api");
                options.Scope.Add("offline_access");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("SampleFront"))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation())
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation())
            .UseOtlpExporter();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<StrictSameSiteExternalAuthenticationMiddleware>();
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api") && context.User.Identity?.IsAuthenticated == false)
            {
                await context.ChallengeAsync();
                return;
            }

            await next();
        });
        app.Map("/api", api =>
        {
            api.RunProxy(async context =>
            {
                var forwardContext = context.ForwardTo(builder.Configuration["BackendAPI"]);

                var token = await context.GetUserAccessTokenAsync();
                forwardContext.UpstreamRequest.SetBearerToken(token);

                return await forwardContext.Send();
            });
        });

        app.MapStaticAssets();
        app.MapRazorPages()
            .WithStaticAssets();
        //app.UseHttpsRedirection();

        app.Run();
    }
}