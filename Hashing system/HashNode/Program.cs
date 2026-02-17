var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

var app = builder.Build();

var storage = new Dictionary<string, string>();

app.MapPost("/put", (KeyValueRequest request) =>
{
    storage[request.Key] = request.Value;
    return Results.Ok();
});

app.MapGet("/get/{key}", (string key) =>
{
    if (storage.TryGetValue(key, out var value))
        return Results.Ok(value);

    return Results.NotFound();
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

record KeyValueRequest(string Key, string Value);
