using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Api;

public static class Configuration
{
    public static void AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        
        services.Configure<JwtBearerOptions>(configuration.GetSection("JwtBearer"));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                                    .RequireAuthenticatedUser()
                                    .Build();
            
            options.AddPolicy("Admin", policy => policy.RequireClaim("role", "admin"));
        });
    }

    public static void ConfigureApp(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/people", () => new[]
        {
            new Person("John", "Doe"),
            new Person("Jane", "Doe"),
            new Person("John", "Smith"),
            new Person("Jane", "Smith")
        }).RequireAuthorization("Admin");
    }
}