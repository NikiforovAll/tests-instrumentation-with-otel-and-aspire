namespace IntegrationTests.Monitoring;

using System.Diagnostics;
using OpenTelemetry;

public class TestRunSpanProcessor(string testRunId) : BaseProcessor<Activity>
{
    private readonly string testRunId = testRunId;

    public override void OnStart(Activity data) => data?.SetTag("test.run_id", this.testRunId);
}
