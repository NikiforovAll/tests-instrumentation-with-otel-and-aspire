namespace IntegrationTests;

using System.Diagnostics;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Monitoring;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Testcontainers.PostgreSql;
using TodoApi;

public class WebAppFixture : IAsyncLifetime
{
    public static ActivitySource ActivitySource { get; } = new(TracerName);
    private const string TracerName = "tests";
    public IAlbaHost AlbaHost { get; private set; } = default!;
    public static Activity ActivityForTestRun { get; private set; } = default!;

    private readonly IContainer aspireDashboard = new ContainerBuilder()
        .WithImage("mcr.microsoft.com/dotnet/aspire-dashboard:8.0.0")
        .WithPortBinding(18888, 18888)
        .WithPortBinding(18889, true)
        .WithEnvironment("DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS", "true")
        .WithWaitStrategy(
            Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(18888))
        )
        .WithReuse(true)
        .WithLabel("aspire-dashboard", "aspire-dashboard-reuse-id")
        .Build();

    private readonly PostgreSqlContainer db = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public async Task InitializeAsync()
    {
        await this.BootstrapAsync();

        ActivityForTestRun = ActivitySource.StartActivity("TestRun")!;
    }

    private async Task BootstrapAsync()
    {
        using var warmupTracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(TracerName)
            .Build();

        using var activityForWarmup = ActivitySource.StartActivity("Warmup")!;

        await this.aspireDashboard.StartAsync();
        activityForWarmup?.AddEvent(new ActivityEvent("AspireDashboard Started."));
        await this.db.StartAsync();
        activityForWarmup?.AddEvent(new ActivityEvent("PostgresSql Started."));

        var otelExporterEndpoint =
            $"http://localhost:{this.aspireDashboard.GetMappedPublicPort(18889)}";

        using var hostActivity = ActivitySource.StartActivity("Start Host")!;

        this.AlbaHost = await Alba.AlbaHost.For<Program>(builder =>
        {
            builder.UseEnvironment("Test");

            builder.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", otelExporterEndpoint);
            builder.UseSetting("OTEL_TRACES_SAMPLER", "always_on");
            builder.UseSetting("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
            builder.UseSetting("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory");
            builder.UseSetting("OTEL_SERVICE_NAME", "test-host");

            builder.UseSetting(
                "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:ConnectionString",
                this.db.GetConnectionString()
            );

            // ordered guid to sort test runs
            var testRunId = NewId.Next().ToString();

            builder.ConfigureServices(services =>
            {
                services
                    .AddOpenTelemetry()
                    .WithTracing(tracing =>
                        tracing
                            .SetResourceBuilder(
                                ResourceBuilder
                                    .CreateDefault()
                                    .AddService(TracerName, serviceInstanceId: testRunId)
                            )
                            .AddSource(TracerName)
                            .AddProcessor(new TestRunSpanProcessor(testRunId))
                    );

                services.AddDbContextFactory<TodoDbContext>();

                services.Configure<JsonOptions>(options =>
                {
                    options.SerializerOptions.WriteIndented = true;
                    options.SerializerOptions.PropertyNamingPolicy =
                        JsonNamingPolicy.SnakeCaseLower;
                });
            });
        });

        await this.AlbaHost.StartAsync();
        activityForWarmup?.AddEvent(new ActivityEvent("Host Started."));
    }

    public async Task DisposeAsync()
    {
        ActivityForTestRun?.Stop();

        await this.AlbaHost.DisposeAsync();
        await this.db.StopAsync();
    }
}

[CollectionDefinition(nameof(WebAppCollection))]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>;

[Collection(nameof(WebAppCollection))]
public abstract class WebAppContext(WebAppFixture fixture)
{
    public IAlbaHost Host { get; } = fixture.AlbaHost;
}
