namespace IntegrationTests;

using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

public class WebAppFixture : IAsyncLifetime
{
    public string OtelExporterEndpoint { get; private set; } = default!;
    public IAlbaHost AlbaHost { get; private set; } = default!;

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

    public async Task InitializeAsync()
    {
        await this.aspireDashboard.StartAsync();

        this.OtelExporterEndpoint =
            $"http://localhost:{this.aspireDashboard.GetMappedPublicPort(18889)}";

        this.AlbaHost = await Alba.AlbaHost.For<Program>(builder =>
        {
            builder.UseEnvironment("Test");

            builder.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", this.OtelExporterEndpoint);
            builder.UseSetting("OTEL_SERVICE_NAME", "test-host");
            builder.UseSetting("OTEL_TRACES_SAMPLER", "always_on");
            builder.UseSetting("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
            builder.UseSetting("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory");

            builder.ConfigureServices(services =>
                services.Configure<JsonOptions>(options =>
                {
                    options.SerializerOptions.WriteIndented = true;
                    options.SerializerOptions.PropertyNamingPolicy =
                        JsonNamingPolicy.SnakeCaseLower;
                })
            );
        });
    }

    public async Task DisposeAsync() => await this.AlbaHost.DisposeAsync();
}

[CollectionDefinition(nameof(WebAppCollection))]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>;

[Collection(nameof(WebAppCollection))]
public abstract class WebAppContext(WebAppFixture fixture)
{
    public IAlbaHost Host { get; } = fixture.AlbaHost;

    public string OtelExporterEndpoint { get; private set; } = fixture.OtelExporterEndpoint;
}
