var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); }));
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

var app = builder.Build();

app.UseSwagger(c =>
{
    c.RouteTemplate = "api/aitranslator/swagger/{documentname}/swagger.json";
});
app.UseDeveloperExceptionPage();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "MyAPI");
    c.RoutePrefix = "api/aitranslator/swagger";
    c.EnableTryItOutByDefault();
});
app.UseCors("MyPolicy");
app.MapGet("api/aitranslator/version", () =>
{
    return Results.Ok(Consts.Version);
});
app.MapGet("api/aitranslator/translate", async (string text, string? source, string target) =>
{
    var result = await AITranslator.AITranslator.TranslateAsync(new[] { text }, target, source);
    if (result != null && result.Length == 1)
    {
        return Results.Ok(result.FirstOrDefault());

    }
    return Results.BadRequest("unable to translate");
});
app.MapPost("api/aitranslator/translate", async (TranslateRequest req) =>
{
    var result = await AITranslator.AITranslator.TranslateAsync(req.Texts, req.Target, req.Source);
    if (result != null && result.Length == 1)
    {
        return Results.Ok(result.FirstOrDefault());

    }
    return Results.BadRequest("unable to translate");
});

app.Run();

public static class Consts
{
    public static string Version = "1.0";
}

public class TranslateRequest
{
    public string[] Texts { get; set; }
    public string? Source { get; set; }
    public string Target { get; set; }
}