using Api.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddJwtApiServices(builder.Configuration);

var app = builder.Build();
app.ConfigureJwtApp();

app.Run();