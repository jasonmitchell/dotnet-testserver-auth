using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Api.Cookie;

public static class Configuration
{
    public static void AddCookieApiServices(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = "api-cookie";
                    options.Cookie.MaxAge = TimeSpan.FromMinutes(15);

                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    };
                });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy("Admin",
                policy => policy.RequireClaim("role", "admin"));

        });
    }

    public static void ConfigureCookieApp(this WebApplication app)
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