using Asp.Versioning;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sample.Api.Middleware;
using Sample.Application;
using Sample.Application.CreateMessage;
using Sample.Application.GetAllMessages;
using Sample.Application.GetMessageById;
using Sample.Application.ProcessMessage;
using Sample.Application.Subscriptions;
using Sample.Domain;
using Sample.Infrastructure.Configuration;
using Sample.Infrastructure.EntityFramework;
using Sample.Infrastructure.Repositories;
using Sample.Infrastructure.Subscriptions;

namespace Sample.Api;

public static class StartupServicesExtensions
{
    public static IServiceCollection AddVersionedApi(this IServiceCollection services, int defaultApiVersion = 1)
    {
        services.AddApiVersioning(options =>
        {
            options.UnsupportedApiVersionStatusCode = 501;
            options.ReportApiVersions = true;
            options.DefaultApiVersion = new ApiVersion(defaultApiVersion, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new HeaderApiVersionReader("X-version"),
                new QueryStringApiVersionReader("api-version"));
        }).AddApiExplorer(options => options.GroupNameFormat = "'v'VVV");
            
        return services;
    }

    public static IServiceCollection AddSwaggerWithVersions(this IServiceCollection services, 
        string apiDescription, int minVersion = 1, int maxVersion = 1)
    {
        return services.AddSwaggerGen(options =>
        {
            foreach (var version in Enumerable.Range(minVersion, (maxVersion - minVersion + 1)))
            {
                options.SwaggerDoc($"v{version}", new OpenApiInfo { Title = apiDescription, Version = $"v{version}" });
            }
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type=ReferenceType.SecurityScheme,
                            Id="Bearer"
                        }
                    }, []
                }
            });
        });
    }

    public static IServiceCollection AddOutboxSupport(
        this IServiceCollection services)
    {
        services.AddDbContext<MessageDbContext>(
            (serviceProvider, optionsBuilder) =>
            {
                var connectionString = serviceProvider.GetRequiredService<OutBoxConfiguration>().Database;
                optionsBuilder.UseSqlite(connectionString);
            });
        services.AddDbContext<IOutboxDbContext, OutboxConsumerDbContext>(
            (serviceProvider, optionsBuilder) =>
            {
                var connectionString = serviceProvider.GetRequiredService<OutBoxConfiguration>().Database;
                optionsBuilder.UseSqlite(connectionString);
            }, ServiceLifetime.Singleton);
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddSingleton<IOutboxRepository, OutboxRepository>();
        services.AddSingleton<RepeatingTimer>();
        services.AddSingleton<IEventConsumer, BusSubscriptionsWithOutbox>();
        return services;
    }

    public static IServiceCollection AddCustomConfiguration(this IServiceCollection services, IConfiguration configurationSection)
    {
        services.Configure<OutBoxConfiguration>(configurationSection);
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<OutBoxConfiguration>>().Value);
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<OutBoxConfiguration>>().Value.Timer);
        return services;
    }

    public static IServiceCollection AddSubscriptions(this IServiceCollection services)
    {
        services.AddSingleton<ISubscribeToContextEvents, MonitoringContextSubscriptions>();
        services.AddSingleton<ISubscribeToContextEvents, NotificationsContextSubscriptions>();
        return services;
    }

    public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<ProcessMessageRequest, ProcessMessageResponse>, ProcessMessageHandler>();
        services.AddScoped<IRequestHandler<GetMessageByIdRequest, GetMessageByIdResponse>, GetMessageByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllMessagesRequest, GetAllMessagesResponse>, GetAllMessagesHandler>();
        services.AddScoped<IRequestHandler<CreateMessageRequest, CreateMessageResponse>, CreateMessageHandler>();
        return services;
    }

    public static void AddConfigurationValidation(this IServiceCollection services)
    {
        services.AddTransient<IStartupFilter, ValidateConfigurationStartupFilter>();
        services.AddSingleton<IEnumerable<IValidateConfiguration>>(serviceProvider =>
        [
            serviceProvider.GetRequiredService<OutBoxConfiguration>(),
            serviceProvider.GetRequiredService<TimerConfiguration>()
        ]);
    }

    public static void AddOpenTelemetryConfiguration(this IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
        }));
        
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        
        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            services.AddOpenTelemetry().UseOtlpExporter();
        }
        
        if (!string.IsNullOrEmpty(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            services.AddOpenTelemetry().UseAzureMonitor();
        }
    }
}