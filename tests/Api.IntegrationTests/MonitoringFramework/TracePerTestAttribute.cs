namespace IntegrationTests.Monitoring;

using System.Diagnostics;
using System.Reflection;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true
)]
public sealed class TracePerTestAttribute : BaseTraceTestAttribute
{
    private Activity? activityForThisTest;

    public override void Before(MethodInfo methodUnderTest)
    {
        var linkToTestRunActivity =
            WebAppFixture.ActivityForTestRun == null
                ? null
                : new List<ActivityLink> { new(WebAppFixture.ActivityForTestRun.Context) };

        this.activityForThisTest = WebAppFixture.ActivitySource.StartActivity(
            methodUnderTest.Name,
            ActivityKind.Internal,
            new ActivityContext(),
            links: linkToTestRunActivity
        );

        base.Before(methodUnderTest);
    }

    public override void After(MethodInfo methodUnderTest)
    {
        this.activityForThisTest?.Stop();
        base.After(methodUnderTest);
    }
}
