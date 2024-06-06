namespace IntegrationTests;

public class ApiTests(WebAppFixture fixture) : WebAppContext(fixture)
{
    [Fact]
    public void Get_DefaultHealthCheck_Ok() =>
        this.Host.Scenario(_ =>
        {
            _.Get.Url("/");
            _.StatusCodeShouldBeOk();
        });

    [Fact]
    public async Task Get_WeatherForecast_Ok()
    {
        var result = await this.Host.GetAsJson<OtelOptions>("/otel");

        result.Should().NotBeNull();
        result!.ExporterEndpoint.Should().Be(this.OtelExporterEndpoint);
    }
}
