using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtBearerOptions>(builder.Configuration.GetSection("JwtBearer"));
builder.Services
       .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer();

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                            .RequireAuthenticatedUser()
                            .Build();
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/people", () => new[]
{
    new Person("John", "Doe"),
    new Person("Jane", "Doe"),
    new Person("John", "Smith"),
    new Person("Jane", "Smith")
}).RequireAuthorization();

app.Run();

record Person(string FirstName, string LastName);