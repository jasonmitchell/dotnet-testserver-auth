using Api.Cookie;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCookieApiServices();

var app = builder.Build();
app.ConfigureCookieApp();

app.Run();