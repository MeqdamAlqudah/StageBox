using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
var app = builder.Build();

var ttl = TimeSpan.FromSeconds(app.Configuration.GetValue("StageTtlSeconds", 900));
var storeDir = Path.Combine(AppContext.BaseDirectory, "store");
app.MapPost("/stage",async (IFormFile file,HttpRequest req,IMemoryCache cache) =>
{
    var userId = req.Headers["X-User-Id"].ToString();
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    byte[] bytes;
    using (var ms = new MemoryStream())
    {
        await file.CopyToAsync(ms);
        bytes = ms.ToArray();
    }
    var fileId = Guid.NewGuid().ToString("N");
    cache.Set($"stage:{userId}:{fileId}",new StagedFile(file.FileName,bytes),ttl);
    return Results.Ok(new {fileId,file.FileName,size=bytes.Length});
}).DisableAntiforgery();

app.Run();

record StagedFile(string FileName, byte[] Bytes);
record SubmitRequest(string FileId);