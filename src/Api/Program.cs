var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet(
        "/otel",
        (IConfiguration configuration) =>
            new OtelOptions(
                configuration["OTEL_EXPORTER_OTLP_ENDPOINT"],
                configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]
            )
    )
    .WithName("OtelLoggerOptions")
    .WithOpenApi();

app.MapDefaultEndpoints();

await app.RunAsync();

public sealed record OtelOptions(string? ExporterEndpoint, string? Protocol);
