using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sample.Api.HealthChecks;
using Sample.Api.Middleware;
using Sample.Infrastructure.EntityFramework;

namespace Sample.Api;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    private const int MinimumApiVersion = 1;
    private const int MaximumApiVersion = 2;
    
    public void ConfigureServices(IServiceCollection services)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["IDP:Authority"];
                options.Audience = "internal-api";
                options.IncludeErrorDetails = environment.IsDevelopment();
                options.RequireHttpsMetadata = !environment.IsDevelopment();
            });
        services.AddControllers();
        services.AddCommandHandlers();
        services.AddVersionedApi(defaultApiVersion: MaximumApiVersion);
        services.AddSwaggerWithVersions("Sample Api", MinimumApiVersion, MaximumApiVersion);
        services.AddCustomConfiguration(configuration.GetSection("OutBox"));
        services.AddConfigurationValidation();
        services.AddOutboxSupport();
        services.AddSubscriptions();
        services.AddHealthChecks()
            .AddDbContextCheck<MessageDbContext>()
            .AddSelfCheck();
        services.AddOpenTelemetryConfiguration();
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
        });
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("SampleApiWithOutbox"))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation())
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation());
        
        services.AddLogging(builder => builder.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        }));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MessageDbContext dbContext)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        app.UseSwaggerWithVersions();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthCheckWithVersion("/health");
            endpoints.MapLivenessProbe("/alive");
        });

        app.UseSubscriptions();
        app.UseMiddleware<LogContextMiddleware>();
    }
}