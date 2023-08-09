using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Sdk;

internal static class HttpTestServerFixture
{
    public static async Task<HttpClient> CreateTestClient(Action<IServiceCollection, IConfiguration>? configureServices = null,
        Action<WebApplication>? configureApp = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        configureServices?.Invoke(builder.Services, builder.Configuration);

        var app = builder.Build();
        configureApp?.Invoke(app);

        await app.StartAsync();

        var testServer = (TestServer)app.Services.GetRequiredService<IServer>();
        var httpClient = testServer.CreateClient();
        return httpClient;
    }
}