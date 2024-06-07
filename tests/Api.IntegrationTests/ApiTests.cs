namespace IntegrationTests;

using System.Net;
using IntegrationTests.Monitoring;

[TracePerTest]
public class ApiTests(WebAppFixture fixture) : WebAppContext(fixture)
{
    [Fact]
    public async Task Get_HealthCheck_Ok() =>
        await this.Host.Scenario(_ =>
        {
            _.Get.Url("/health");
            _.StatusCodeShouldBeOk();
        });

    [Fact]
    public async Task Get_SwaggerInNonDevelopmentEnvironment_NotFound() =>
        await this.Host.Scenario(_ =>
        {
            _.Get.Url("/swagger");
            _.StatusCodeShouldBe(HttpStatusCode.NotFound);
        });
}
