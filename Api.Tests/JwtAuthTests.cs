using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests;

public class JwtAuthTests
{
    [Fact]
    public async Task User_is_authenticated_by_test_token()
    {
        var httpClient = await CreateTestServer();
        var jwt = CreateTestJwt("admin");
        
        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/people")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwt)
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task Authorization_policy_is_applied_with_test_token()
    {
        var httpClient = await CreateTestServer();
        var jwt = CreateTestJwt("dev");
        
        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/people")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, jwt)
            }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<HttpClient> CreateTestServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddApiServices(builder.Configuration);

        ConfigureAuthenticationForTests(builder);

        var app = builder.Build();
        app.ConfigureApp();

        await app.StartAsync();

        var testServer = (TestServer)app.Services.GetRequiredService<IServer>();
        var httpClient = testServer.CreateClient();
        return httpClient;
    }

    private static void ConfigureAuthenticationForTests(WebApplicationBuilder builder)
    {
        // Remove the existing configuration from the API, we don't need that...
        builder.Services.RemoveAll<IPostConfigureOptions<JwtBearerOptions>>();
        
        // Reconfigure JwtBearerOptions to use a custom token validator
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                SignatureValidator = (token, _) => new JwtSecurityToken(token), ValidateAudience = false, ValidateIssuer = false
            };
        });
    }

    private static string CreateTestJwt(string role)
    {
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(1),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(RSA.Create()), SecurityAlgorithms.RsaSha512),
            Subject = new ClaimsIdentity(new List<Claim>
            {
                new("name", "Some User"), new("role", role)
            })
        };

        var securityTokenHandler = new JwtSecurityTokenHandler();
        var token = securityTokenHandler.CreateToken(securityTokenDescriptor);
        var encodedAccessToken = securityTokenHandler.WriteToken(token);

        return encodedAccessToken;
    }
}