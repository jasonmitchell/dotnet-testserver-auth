using System.Net;
using System.Security.Claims;
using Api.Cookie;
using Api.Tests.Sdk;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Api.Tests;

public class CookieAuthTests
{
    [Fact]
    public async Task User_is_authenticated_by_test_cookie()
    {
        var httpClient = await CreateTestClient();
        var cookie = CreateCookie("admin");

        var request = new HttpRequestMessage(HttpMethod.Get, "/people");
        request.Headers.Add("cookie", cookie.ToString());
        
        var response = await httpClient.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    private static async Task<HttpClient> CreateTestClient()
    {
        return await HttpTestServerFixture.CreateTestClient(configureServices: (services, _) =>
            {
                services.AddCookieApiServices();
                ConfigureAuthenticationForTest(services);
            },
            configureApp: app => app.ConfigureCookieApp());
    }
    
    private static void ConfigureAuthenticationForTest(IServiceCollection services)
    {
        // Remove the existing configuration from the API, we don't need that...
        services.RemoveAll<IPostConfigureOptions<CookieAuthenticationOptions>>();
        
        // Reconfigure CookieAuthenticationOptions to use a custom cookie format
        services.PostConfigure(CookieAuthenticationDefaults.AuthenticationScheme, ConfigureCookieAuthenticationOptions);
    }
    
    private static Action<CookieAuthenticationOptions> ConfigureCookieAuthenticationOptions => options =>
    {
        options.CookieManager = new ChunkingCookieManager();
        options.TicketDataFormat = new TestCookieTicketFormat();

        options.Cookie.Name = "api-cookie";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    };
    
    private static CookieHeaderValue CreateCookie(string role)
    {
        var claims = new List<Claim>
        {
            new("name", "Some User"), new("role", role)
        };
        
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var authenticationTicket = new AuthenticationTicket(claimsPrincipal, CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Use the test cookie ticket format to "protect" the authentication ticket and create the cookie value
        var cookie = new TestCookieTicketFormat().Protect(authenticationTicket);
        
        // Make sure to use the same cookie name as the API is expecting defined above
        return new CookieHeaderValue("api-cookie", cookie);
    }
}