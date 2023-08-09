using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Api.Jwt;
using Api.Tests.Sdk;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        var httpClient = await CreateTestClient();
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
        var httpClient = await CreateTestClient();
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

    private static async Task<HttpClient> CreateTestClient()
    {
        return await HttpTestServerFixture.CreateTestClient(configureServices: (services, config) =>
            {
                services.AddJwtApiServices(config);
                ConfigureAuthenticationForTests(services);
            },
            configureApp: app => app.ConfigureJwtApp());
    }

    private static void ConfigureAuthenticationForTests(IServiceCollection services)
    {
        // Remove the existing configuration from the API, we don't need that...
        services.RemoveAll<IPostConfigureOptions<JwtBearerOptions>>();
        
        // Reconfigure JwtBearerOptions to use a custom token validator
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
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