using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Application;
using Sample.Application.CreateMessage;
using Sample.Application.GetAllMessages;
using Sample.Application.GetMessageById;
using Sample.Application.ProcessMessage;

namespace Sample.Api.Tests;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public IRequestHandler<GetAllMessagesRequest, GetAllMessagesResponse> GetAllMessagesHandler { get; set; }

    public IRequestHandler<GetMessageByIdRequest, GetMessageByIdResponse> GetMessageByIdHandler { get; set; }

    public IRequestHandler<CreateMessageRequest, CreateMessageResponse> CreateMessageHandler { get; set; }

    public IRequestHandler<ProcessMessageRequest, ProcessMessageResponse> ProcessMessageHandler { get; set; }
        
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var projectDir = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(projectDir, "appsettings.json");
        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            configurationBuilder.AddJsonFile(configPath);
        });
        builder.ConfigureServices(services =>
        {
            var getAllMessagesDescriptor = services.SingleOrDefault(s =>
                s.ServiceType == typeof(IRequestHandler<GetAllMessagesRequest, GetAllMessagesResponse>));
            services.Remove(getAllMessagesDescriptor);
            services.AddScoped(_ => GetAllMessagesHandler);

            var getMessageByIdDescriptor = services.SingleOrDefault(s =>
                s.ServiceType == typeof(IRequestHandler<GetMessageByIdRequest, GetMessageByIdResponse>));
            services.Remove(getMessageByIdDescriptor);
            services.AddScoped(_ => GetMessageByIdHandler);

            var createMessageDescriptor = services.SingleOrDefault(s => 
                s.ServiceType == typeof(IRequestHandler<CreateMessageRequest, CreateMessageResponse>));
            services.Remove(createMessageDescriptor);
            services.AddScoped(_ => CreateMessageHandler);

            var processMessageDescriptor = services.SingleOrDefault(s =>
                s.ServiceType == typeof(IRequestHandler<ProcessMessageRequest, ProcessMessageResponse>));
            services.Remove(processMessageDescriptor);
            services.AddScoped(_ => ProcessMessageHandler);

            services.AddAuthentication(options => options.DefaultScheme = "Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => options.ForwardDefault = "Test");
        });
    }
}

public class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.ContainsKey("Authorization"))
		{
			return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
		}
        var identity = new ClaimsIdentity([new Claim("key", "value")], "Test");
		return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), null, "Test")));
	}
}