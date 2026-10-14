using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
var app = builder.Build();

var ttl = TimeSpan.FromSeconds(app.Configuration.GetValue("StageTtlSeconds", 900));
var storeDir = Path.Combine(AppContext.BaseDirectory, "store");
Directory.CreateDirectory(storeDir);
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

app.MapPost("/submit",(SubmitRequest body , HttpRequest req , IMemoryCache cache) =>
{
    var userId = req.Headers["X-User-Id"].ToString();
    var key = $"stage: {userId}: {body.FileId} ";
    if (!cache.TryGetValue<StagedFile>(key, out var staged) || staged is null)
        return Results.NotFound(new {error ="staged file not found or expired",
        server = Environment.MachineName});
    var agreementId = Guid.NewGuid().ToString("N");
    File.WriteAllBytes(Path.Combine(storeDir, agreementId + " .pdf"), staged.Bytes);
    cache.Remove(key);
    return Results.Ok(new {agreementId,server=Environment.MachineName,pid=Environment.ProcessId});
});

app.MapGet("/whoami", () => new
{
    server = Environment.MachineName,
    pid = Environment.ProcessId
});
app.Run();

record StagedFile(string FileName, byte[] Bytes);
record SubmitRequest(string FileId);