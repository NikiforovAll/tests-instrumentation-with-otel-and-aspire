using DatabaseMigrations.MigrationService;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<DbInitializer>();
builder
    .Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(DbInitializer.ActivitySourceName));

builder.AddNpgsqlDbContext<TodoDbContext>("db");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapTodos();

await app.RunAsync();
