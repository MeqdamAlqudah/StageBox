using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
builder.Services.AddMemoryCache();
var ttl = TimeSpan.FromSeconds(30);
app.MapGet("/", () => "Hello World!");

app.Run();
